using Alsappan.Application.Search.Repositories;
using Alsappan.Application.Timeline;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Inspections;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Payments;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Search;

public sealed class EfGlobalSearchRepository : IGlobalSearchRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfGlobalSearchRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<IReadOnlyList<GlobalSearchResultRecord>> SearchAsync(
    OrganizationId organizationId,
    GlobalSearchQuery query,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(query);

    var results = new List<GlobalSearchResultRecord>();

    if (CanRead(query, "property"))
    {
      results.AddRange(await SearchPropertiesAsync(organizationId, query, cancellationToken).ConfigureAwait(false));
    }

    if (CanRead(query, "resident"))
    {
      results.AddRange(await SearchResidentsAsync(organizationId, query, cancellationToken).ConfigureAwait(false));
    }

    if (CanRead(query, "contract"))
    {
      results.AddRange(await SearchContractsAsync(organizationId, query, cancellationToken).ConfigureAwait(false));
    }

    if (CanRead(query, "payment"))
    {
      results.AddRange(await SearchPaymentsAsync(organizationId, query, cancellationToken).ConfigureAwait(false));
    }

    if (CanRead(query, "document"))
    {
      results.AddRange(await SearchDocumentsAsync(organizationId, query, cancellationToken).ConfigureAwait(false));
    }

    if (CanRead(query, "occurrence"))
    {
      results.AddRange(await SearchOccurrencesAsync(organizationId, query, cancellationToken).ConfigureAwait(false));
    }

    if (CanRead(query, "inspection"))
    {
      results.AddRange(await SearchInspectionsAsync(organizationId, query, cancellationToken).ConfigureAwait(false));
    }

    return results
      .OrderBy(result => Array.IndexOf(KnownEntityTypes, result.EntityType))
      .ThenByDescending(result => result.SortDate)
      .ToArray();
  }

  private async Task<IReadOnlyList<GlobalSearchResultRecord>> SearchPropertiesAsync(
    OrganizationId organizationId,
    GlobalSearchQuery query,
    CancellationToken cancellationToken)
  {
    var search = PropertyCode.NormalizeSearchText(query.Query);
    var rows = await dbContext.Properties.IgnoreQueryFilters()
      .Where(property => property.OrganizationId == organizationId &&
        property.DeletedAt == null &&
        property.Status != PropertyStatus.Archived &&
        property.SearchText.Contains(search))
      .OrderByDescending(property => property.UpdatedAt ?? property.CreatedAt)
      .ThenBy(property => property.Name)
      .Take(query.LimitPerEntity)
      .Select(property => new
      {
        property.Id,
        property.Name,
        property.SearchText,
        property.CreatedAt,
        property.UpdatedAt,
        property.Address.StreetLine,
        property.Address.Number,
        property.Address.Neighborhood,
        property.Address.City,
        property.Address.StateCode
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(row =>
      new GlobalSearchResultRecord(
        "property",
        row.Id.Value,
        row.Name,
        $"{row.StreetLine}, {row.Number} - {row.Neighborhood}, {row.City}/{row.StateCode}",
        MatchPropertyField(row.Name, row.SearchText, search),
        TimelineCatalog.GetEntityRoute("property", row.Id.Value.ToString("D")) ?? "/imoveis",
        row.UpdatedAt ?? row.CreatedAt))
      .ToArray();
  }

  private async Task<IReadOnlyList<GlobalSearchResultRecord>> SearchResidentsAsync(
    OrganizationId organizationId,
    GlobalSearchQuery query,
    CancellationToken cancellationToken)
  {
    var search = ResidentCode.NormalizeSearchText(query.Query);
    var rows = await dbContext.Residents.IgnoreQueryFilters()
      .Where(resident => resident.OrganizationId == organizationId &&
        resident.DeletedAt == null &&
        resident.Status != ResidentStatus.Archived &&
        resident.SearchText.Contains(search))
      .OrderByDescending(resident => resident.UpdatedAt ?? resident.CreatedAt)
      .ThenBy(resident => resident.FullName)
      .Take(query.LimitPerEntity)
      .Select(resident => new
      {
        resident.Id,
        resident.FullName,
        resident.Email,
        resident.Phone,
        resident.DocumentIdentifier,
        resident.SearchText,
        resident.CreatedAt,
        resident.UpdatedAt
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(row =>
      new GlobalSearchResultRecord(
        "resident",
        row.Id.Value,
        row.FullName,
        FirstNonEmpty(row.Email, row.Phone, row.DocumentIdentifier),
        MatchResidentField(row.FullName, row.Email, row.Phone, row.DocumentIdentifier, row.SearchText, search),
        TimelineCatalog.GetEntityRoute("resident", row.Id.Value.ToString("D")) ?? "/moradores",
        row.UpdatedAt ?? row.CreatedAt))
      .ToArray();
  }

  private async Task<IReadOnlyList<GlobalSearchResultRecord>> SearchContractsAsync(
    OrganizationId organizationId,
    GlobalSearchQuery query,
    CancellationToken cancellationToken)
  {
    var search = ContractCode.NormalizeSearchText(query.Query);
    var rows = await dbContext.Contracts.IgnoreQueryFilters()
      .Where(contract => contract.OrganizationId == organizationId &&
        contract.DeletedAt == null &&
        contract.Status != ContractStatus.Archived &&
        contract.SearchText.Contains(search))
      .OrderByDescending(contract => contract.UpdatedAt ?? contract.CreatedAt)
      .ThenByDescending(contract => contract.StartDate)
      .Take(query.LimitPerEntity)
      .Select(contract => new
      {
        contract.Id,
        contract.SearchText,
        contract.StartDate,
        contract.EndDate,
        contract.Status,
        contract.CreatedAt,
        contract.UpdatedAt
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(row =>
      new GlobalSearchResultRecord(
        "contract",
        row.Id.Value,
        $"Contrato {row.StartDate:yyyy-MM}",
        row.EndDate.HasValue ? $"{row.StartDate:yyyy-MM-dd} - {row.EndDate:yyyy-MM-dd}" : $"{row.StartDate:yyyy-MM-dd}",
        MatchStatusOrDate(row.SearchText, search),
        TimelineCatalog.GetEntityRoute("contract", row.Id.Value.ToString("D")) ?? "/contratos",
        row.UpdatedAt ?? row.CreatedAt))
      .ToArray();
  }

  private async Task<IReadOnlyList<GlobalSearchResultRecord>> SearchPaymentsAsync(
    OrganizationId organizationId,
    GlobalSearchQuery query,
    CancellationToken cancellationToken)
  {
    var search = PaymentCode.NormalizeSearchText(query.Query);
    var rows = await dbContext.PaymentCharges.IgnoreQueryFilters()
      .Where(charge => charge.OrganizationId == organizationId &&
        charge.DeletedAt == null &&
        charge.Status != PaymentStatus.Archived &&
        charge.SearchText.Contains(search))
      .OrderByDescending(charge => charge.UpdatedAt ?? charge.CreatedAt)
      .ThenBy(charge => charge.DueDate)
      .Take(query.LimitPerEntity)
      .Select(charge => new
      {
        charge.Id,
        charge.Title,
        charge.Description,
        charge.SearchText,
        charge.DueDate,
        charge.CreatedAt,
        charge.UpdatedAt
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(row =>
      new GlobalSearchResultRecord(
        "payment",
        row.Id.Value,
        row.Title,
        row.Description ?? $"Vencimento {row.DueDate:yyyy-MM-dd}",
        MatchNameOrDate(row.Title, row.SearchText, search),
        TimelineCatalog.GetEntityRoute("payment", row.Id.Value.ToString("D")) ?? "/pagamentos",
        row.UpdatedAt ?? row.CreatedAt))
      .ToArray();
  }

  private async Task<IReadOnlyList<GlobalSearchResultRecord>> SearchDocumentsAsync(
    OrganizationId organizationId,
    GlobalSearchQuery query,
    CancellationToken cancellationToken)
  {
    var search = DocumentCode.NormalizeSearchText(query.Query);
    var rows = await dbContext.Documents.IgnoreQueryFilters()
      .Where(document => document.OrganizationId == organizationId &&
        document.DeletedAt == null &&
        document.Status != DocumentStatus.Archived &&
        document.SearchText.Contains(search))
      .OrderByDescending(document => document.UpdatedAt ?? document.CreatedAt)
      .ThenBy(document => document.Title)
      .Take(query.LimitPerEntity)
      .Select(document => new
      {
        document.Id,
        document.Title,
        document.Description,
        document.CurrentFileName,
        document.SearchText,
        document.CreatedAt,
        document.UpdatedAt
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(row =>
      new GlobalSearchResultRecord(
        "document",
        row.Id.Value,
        row.Title,
        row.Description ?? row.CurrentFileName,
        MatchDocumentField(row.Title, row.CurrentFileName, row.SearchText, search),
        TimelineCatalog.GetEntityRoute("document", row.Id.Value.ToString("D")) ?? "/documentos",
        row.UpdatedAt ?? row.CreatedAt))
      .ToArray();
  }

  private async Task<IReadOnlyList<GlobalSearchResultRecord>> SearchOccurrencesAsync(
    OrganizationId organizationId,
    GlobalSearchQuery query,
    CancellationToken cancellationToken)
  {
    var search = OccurrenceCode.NormalizeSearchText(query.Query);
    var rows = await dbContext.Occurrences.IgnoreQueryFilters()
      .Where(occurrence => occurrence.OrganizationId == organizationId &&
        occurrence.DeletedAt == null &&
        occurrence.Status != OccurrenceStatus.Archived &&
        occurrence.SearchText.Contains(search))
      .OrderByDescending(occurrence => occurrence.UpdatedAt ?? occurrence.CreatedAt)
      .ThenBy(occurrence => occurrence.Title)
      .Take(query.LimitPerEntity)
      .Select(occurrence => new
      {
        occurrence.Id,
        occurrence.Title,
        occurrence.Description,
        occurrence.SearchText,
        occurrence.Status,
        occurrence.Priority,
        occurrence.CreatedAt,
        occurrence.UpdatedAt
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(row =>
      new GlobalSearchResultRecord(
        "occurrence",
        row.Id.Value,
        row.Title,
        row.Description,
        MatchNameOrStatus(row.Title, row.SearchText, search),
        TimelineCatalog.GetEntityRoute("occurrence", row.Id.Value.ToString("D")) ?? "/ocorrencias",
        row.UpdatedAt ?? row.CreatedAt))
      .ToArray();
  }

  private async Task<IReadOnlyList<GlobalSearchResultRecord>> SearchInspectionsAsync(
    OrganizationId organizationId,
    GlobalSearchQuery query,
    CancellationToken cancellationToken)
  {
    var search = InspectionCode.NormalizeSearchText(query.Query);
    var rows = await dbContext.Inspections.IgnoreQueryFilters()
      .Where(inspection => inspection.OrganizationId == organizationId &&
        inspection.DeletedAt == null &&
        inspection.Status != InspectionStatus.Archived &&
        inspection.SearchText.Contains(search))
      .OrderByDescending(inspection => inspection.UpdatedAt ?? inspection.CreatedAt)
      .ThenBy(inspection => inspection.ScheduledAt)
      .Take(query.LimitPerEntity)
      .Select(inspection => new
      {
        inspection.Id,
        inspection.Title,
        inspection.Notes,
        inspection.SearchText,
        inspection.ScheduledAt,
        inspection.CreatedAt,
        inspection.UpdatedAt
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(row =>
      new GlobalSearchResultRecord(
        "inspection",
        row.Id.Value,
        row.Title,
        row.Notes ?? $"Agendada em {row.ScheduledAt:yyyy-MM-dd}",
        MatchNameOrDate(row.Title, row.SearchText, search),
        TimelineCatalog.GetEntityRoute("inspection", row.Id.Value.ToString("D")) ?? "/vistorias",
        row.UpdatedAt ?? row.CreatedAt))
      .ToArray();
  }

  private static bool CanRead(GlobalSearchQuery query, string entityType) =>
    query.ReadableEntityTypes is null ||
    query.ReadableEntityTypes.Contains(TimelineCatalog.NormalizeEntityType(entityType));

  private static string MatchPropertyField(string name, string searchText, string search) =>
    ContainsNormalized(name, search, value => PropertyCode.NormalizeSearchText(value))
      ? "name"
      : searchText.Contains(search, StringComparison.Ordinal)
        ? "address"
        : "searchText";

  private static string MatchResidentField(
    string fullName,
    string? email,
    string? phone,
    string? document,
    string searchText,
    string search)
  {
    if (ContainsNormalized(fullName, search, value => ResidentCode.NormalizeSearchText(value)))
    {
      return "name";
    }

    if (ContainsNormalized(email, search, value => ResidentCode.NormalizeSearchText(value)) ||
      ContainsNormalized(phone, search, value => ResidentCode.NormalizeSearchText(value)))
    {
      return "contact";
    }

    return ContainsNormalized(document, search, value => ResidentCode.NormalizeSearchText(value)) ? "document" : "searchText";
  }

  private static string MatchDocumentField(string title, string fileName, string searchText, string search) =>
    ContainsNormalized(title, search, value => DocumentCode.NormalizeSearchText(value))
      ? "name"
      : ContainsNormalized(fileName, search, value => DocumentCode.NormalizeSearchText(value))
        ? "document"
        : searchText.Contains(search, StringComparison.Ordinal)
          ? "searchText"
          : "searchText";

  private static string MatchNameOrStatus(string title, string searchText, string search) =>
    ContainsNormalized(title, search, value => OccurrenceCode.NormalizeSearchText(value))
      ? "name"
      : searchText.Contains(search, StringComparison.Ordinal)
        ? "status"
        : "searchText";

  private static string MatchNameOrDate(string title, string searchText, string search) =>
    ContainsNormalized(title, search, value => PaymentCode.NormalizeSearchText(value))
      ? "name"
      : MatchStatusOrDate(searchText, search);

  private static string MatchStatusOrDate(string searchText, string search) =>
    search.Any(char.IsDigit) && searchText.Contains(search, StringComparison.Ordinal) ? "date" : "status";

  private static bool ContainsNormalized(
    string? value,
    string search,
    Func<string?, string> normalize) =>
    !string.IsNullOrWhiteSpace(value) && normalize(value).Contains(search, StringComparison.Ordinal);

  private static string? FirstNonEmpty(params string?[] values) =>
    values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

  private static readonly string[] KnownEntityTypes =
  [
    "property",
    "resident",
    "contract",
    "payment",
    "document",
    "occurrence",
    "inspection"
  ];
}
