using Alsappan.Application.Dashboard.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Inspections;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Payments;
using Alsappan.Domain.Properties;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Dashboard;

public sealed class EfDashboardRepository : IDashboardRepository
{
  private const int PendingInspectionWindowDays = 7;

  private readonly AlsappanDbContext dbContext;

  public EfDashboardRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<DashboardMetricsSnapshot> GetOverviewAsync(
    OrganizationId organizationId,
    DateOnly today,
    DateTimeOffset now,
    DashboardQueryPermissions permissions,
    int recentActivityLimit,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(permissions);

    var occupancy = permissions.CanReadProperties
      ? await GetOccupancyAsync(organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var overduePayments = permissions.CanReadPayments
      ? await GetOverduePaymentsAsync(organizationId, today, cancellationToken).ConfigureAwait(false)
      : null;
    var contractExpirations = permissions.CanReadContracts
      ? await GetContractExpirationsAsync(organizationId, today, cancellationToken).ConfigureAwait(false)
      : null;
    var openOccurrences = permissions.CanReadOccurrences
      ? await GetOpenOccurrencesAsync(organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var pendingInspections = permissions.CanReadInspections
      ? await GetPendingInspectionsAsync(organizationId, now, cancellationToken).ConfigureAwait(false)
      : null;
    var recentActivity = permissions.CanReadTimeline
      ? await GetRecentActivityAsync(
          organizationId,
          permissions.ReadableActivityEntityTypes,
          recentActivityLimit,
          cancellationToken)
        .ConfigureAwait(false)
      : [];

    return new DashboardMetricsSnapshot(
      occupancy,
      overduePayments,
      contractExpirations,
      openOccurrences,
      pendingInspections,
      recentActivity);
  }

  private async Task<DashboardOccupancySnapshot> GetOccupancyAsync(
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var query = dbContext.Properties.IgnoreQueryFilters()
      .Where(property => property.OrganizationId == organizationId &&
        property.DeletedAt == null &&
        property.Status != PropertyStatus.Archived);
    var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var rented = await query.CountAsync(
        property => property.Status == PropertyStatus.Rented,
        cancellationToken)
      .ConfigureAwait(false);

    return new DashboardOccupancySnapshot(total, rented);
  }

  private async Task<DashboardOverduePaymentSnapshot> GetOverduePaymentsAsync(
    OrganizationId organizationId,
    DateOnly today,
    CancellationToken cancellationToken)
  {
    var charges = await dbContext.PaymentCharges.IgnoreQueryFilters()
      .Include(charge => charge.Transactions)
      .Where(charge => charge.OrganizationId == organizationId &&
        charge.DeletedAt == null &&
        charge.Status != PaymentStatus.Archived &&
        charge.Status != PaymentStatus.Cancelled &&
        charge.Status != PaymentStatus.Disputed &&
        charge.DueDate < today)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var overdue = charges
      .Where(charge => charge.GetEffectiveStatus(today) == PaymentStatus.Overdue)
      .ToArray();
    var currency = overdue.Select(charge => charge.Amount.Currency).FirstOrDefault() ?? "BRL";
    var balance = overdue
      .Where(charge => string.Equals(charge.Amount.Currency, currency, StringComparison.Ordinal))
      .Sum(charge => charge.Balance().Amount);

    return new DashboardOverduePaymentSnapshot(overdue.Length, balance, currency);
  }

  private async Task<DashboardContractExpirationSnapshot> GetContractExpirationsAsync(
    OrganizationId organizationId,
    DateOnly today,
    CancellationToken cancellationToken)
  {
    var endWindow = today.AddDays(LeaseContract.DefaultEndingSoonDays);
    var query = dbContext.Contracts.IgnoreQueryFilters()
      .Where(contract => contract.OrganizationId == organizationId &&
        contract.DeletedAt == null &&
        contract.Status == ContractStatus.Active &&
        contract.EndDate.HasValue &&
        contract.EndDate.Value >= today &&
        contract.EndDate.Value <= endWindow);
    var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var nextExpiration = await query
      .OrderBy(contract => contract.EndDate)
      .Select(contract => contract.EndDate)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    return new DashboardContractExpirationSnapshot(count, nextExpiration);
  }

  private async Task<DashboardOpenOccurrenceSnapshot> GetOpenOccurrencesAsync(
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var query = dbContext.Occurrences.IgnoreQueryFilters()
      .Where(occurrence => occurrence.OrganizationId == organizationId &&
        occurrence.DeletedAt == null &&
        occurrence.Status != OccurrenceStatus.Resolved &&
        occurrence.Status != OccurrenceStatus.Cancelled &&
        occurrence.Status != OccurrenceStatus.Archived);
    var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var urgent = await query.CountAsync(
        occurrence => occurrence.Priority == OccurrencePriority.Urgent,
        cancellationToken)
      .ConfigureAwait(false);

    return new DashboardOpenOccurrenceSnapshot(count, urgent);
  }

  private async Task<DashboardPendingInspectionSnapshot> GetPendingInspectionsAsync(
    OrganizationId organizationId,
    DateTimeOffset now,
    CancellationToken cancellationToken)
  {
    var pendingUntil = now.AddDays(PendingInspectionWindowDays);
    var query = dbContext.Inspections.IgnoreQueryFilters()
      .Where(inspection => inspection.OrganizationId == organizationId &&
        inspection.DeletedAt == null &&
        (inspection.Status == InspectionStatus.Scheduled || inspection.Status == InspectionStatus.InProgress) &&
        inspection.ScheduledAt <= pendingUntil);
    var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var nextScheduledAt = await query
      .OrderBy(inspection => inspection.ScheduledAt)
      .Select(inspection => (DateTimeOffset?)inspection.ScheduledAt)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    return new DashboardPendingInspectionSnapshot(count, nextScheduledAt);
  }

  private async Task<IReadOnlyList<DashboardActivitySnapshot>> GetRecentActivityAsync(
    OrganizationId organizationId,
    IReadOnlySet<string>? readableEntityTypes,
    int recentActivityLimit,
    CancellationToken cancellationToken)
  {
    if (readableEntityTypes is { Count: 0 })
    {
      return [];
    }

    var query = dbContext.TimelineEntries.IgnoreQueryFilters()
      .Where(entry => entry.OrganizationId == organizationId);

    if (readableEntityTypes is not null)
    {
      var readableTypes = readableEntityTypes.ToArray();
      query = query.Where(entry => readableTypes.Contains(entry.SubjectEntityType));
    }

    return await query
      .OrderByDescending(entry => entry.OccurredAt)
      .ThenByDescending(entry => entry.Id)
      .Take(Math.Max(1, recentActivityLimit))
      .Select(entry => new DashboardActivitySnapshot(
        entry.Id,
        entry.EventName,
        entry.OccurredAt,
        entry.SubjectEntityType,
        entry.SubjectEntityId,
        entry.SubjectDisplayName))
      .ToArrayAsync(cancellationToken)
      .ConfigureAwait(false);
  }
}
