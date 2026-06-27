using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Common.ValueObjects;

namespace Alsappan.Domain.Contracts;

public sealed class LeaseContract : TenantScopedEntity<EntityId>
{
  public const int DefaultEndingSoonDays = 30;

  private readonly List<ContractResident> residents = [];

  private LeaseContract()
  {
  }

  private LeaseContract(
    EntityId id,
    OrganizationId organizationId,
    EntityId propertyId,
    EntityId primaryResidentId,
    IEnumerable<EntityId> residentIds,
    DateOnly startDate,
    DateOnly? endDate,
    Money monthlyRent,
    int dueDay,
    Money? depositAmount,
    ContractAdjustmentIndex adjustmentIndex,
    int adjustmentIntervalMonths,
    DateOnly? nextAdjustmentDate,
    string? penaltyNotes,
    string? discountNotes,
    bool generatePaymentsAutomatically,
    string? notes,
    string propertySearchText,
    string residentSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    PropertyId = propertyId;
    ApplyTerms(
      primaryResidentId,
      residentIds,
      startDate,
      endDate,
      monthlyRent,
      dueDay,
      depositAmount,
      adjustmentIndex,
      adjustmentIntervalMonths,
      nextAdjustmentDate,
      penaltyNotes,
      discountNotes,
      generatePaymentsAutomatically,
      notes,
      propertySearchText,
      residentSearchText,
      createdAt,
      createdByUserId);
    Status = ContractStatus.Draft;
  }

  public EntityId PropertyId { get; private set; }

  public EntityId PrimaryResidentId { get; private set; }

  public IReadOnlyCollection<ContractResident> Residents => residents.AsReadOnly();

  public ContractStatus Status { get; private set; }

  public DateOnly StartDate { get; private set; }

  public DateOnly? EndDate { get; private set; }

  public Money MonthlyRent { get; private set; } = null!;

  public int DueDay { get; private set; }

  public Money? DepositAmount { get; private set; }

  public ContractAdjustmentIndex AdjustmentIndex { get; private set; }

  public int AdjustmentIntervalMonths { get; private set; }

  public DateOnly? NextAdjustmentDate { get; private set; }

  public string? PenaltyNotes { get; private set; }

  public string? DiscountNotes { get; private set; }

  public bool GeneratePaymentsAutomatically { get; private set; }

  public string? Notes { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public static LeaseContract Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId propertyId,
    EntityId primaryResidentId,
    IEnumerable<EntityId> residentIds,
    DateOnly startDate,
    DateOnly? endDate,
    Money monthlyRent,
    int dueDay,
    Money? depositAmount,
    ContractAdjustmentIndex adjustmentIndex,
    int adjustmentIntervalMonths,
    DateOnly? nextAdjustmentDate,
    string? penaltyNotes,
    string? discountNotes,
    bool generatePaymentsAutomatically,
    string? notes,
    string propertySearchText,
    string residentSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      propertyId,
      primaryResidentId,
      residentIds,
      startDate,
      endDate,
      monthlyRent,
      dueDay,
      depositAmount,
      adjustmentIndex,
      adjustmentIntervalMonths,
      nextAdjustmentDate,
      penaltyNotes,
      discountNotes,
      generatePaymentsAutomatically,
      notes,
      propertySearchText,
      residentSearchText,
      createdAt,
      createdByUserId);

  public void Update(
    EntityId primaryResidentId,
    IEnumerable<EntityId> residentIds,
    DateOnly startDate,
    DateOnly? endDate,
    Money monthlyRent,
    int dueDay,
    Money? depositAmount,
    ContractAdjustmentIndex adjustmentIndex,
    int adjustmentIntervalMonths,
    DateOnly? nextAdjustmentDate,
    string? penaltyNotes,
    string? discountNotes,
    bool generatePaymentsAutomatically,
    string? notes,
    string propertySearchText,
    string residentSearchText,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    ArgumentNullException.ThrowIfNull(monthlyRent);

    EnsureCanMutateTerms();
    ApplyTerms(
      primaryResidentId,
      residentIds,
      startDate,
      endDate,
      monthlyRent,
      dueDay,
      depositAmount,
      adjustmentIndex,
      adjustmentIntervalMonths,
      nextAdjustmentDate,
      penaltyNotes,
      discountNotes,
      generatePaymentsAutomatically,
      notes,
      propertySearchText,
      residentSearchText,
      updatedAt,
      updatedByUserId);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Activate(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (Status is ContractStatus.Archived or ContractStatus.Cancelled or ContractStatus.Terminated)
    {
      throw new InvalidOperationException("Contract cannot be activated from its current status.");
    }

    Status = ContractStatus.Active;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Terminate(DateOnly effectiveDate, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (Status != ContractStatus.Active)
    {
      throw new InvalidOperationException("Only active contracts can be terminated.");
    }

    if (effectiveDate < StartDate)
    {
      throw new ArgumentException("Termination date cannot be before the start date.", nameof(effectiveDate));
    }

    Status = ContractStatus.Terminated;
    EndDate = effectiveDate;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Cancel(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (Status is ContractStatus.Archived or ContractStatus.Terminated or ContractStatus.Cancelled)
    {
      return;
    }

    Status = ContractStatus.Cancelled;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    Status = ContractStatus.Archived;
    MarkDeleted(deletedAt, deletedByUserId);
  }

  public void Restore(DateTimeOffset restoredAt, UserId? restoredByUserId)
  {
    if (!IsDeleted)
    {
      return;
    }

    DeletedAt = null;
    DeletedByUserId = null;
    Status = ContractStatus.Draft;
    MarkUpdated(restoredAt, restoredByUserId);
  }

  public ContractStatus GetEffectiveStatus(DateOnly today, int endingSoonDays = DefaultEndingSoonDays)
  {
    if (IsDeleted || Status == ContractStatus.Archived)
    {
      return ContractStatus.Archived;
    }

    if (Status is not ContractStatus.Active || EndDate is null)
    {
      return Status;
    }

    if (EndDate.Value < today)
    {
      return ContractStatus.Ended;
    }

    return EndDate.Value <= today.AddDays(endingSoonDays)
      ? ContractStatus.EndingSoon
      : ContractStatus.Active;
  }

  private void ApplyTerms(
    EntityId primaryResidentId,
    IEnumerable<EntityId> residentIds,
    DateOnly startDate,
    DateOnly? endDate,
    Money monthlyRent,
    int dueDay,
    Money? depositAmount,
    ContractAdjustmentIndex adjustmentIndex,
    int adjustmentIntervalMonths,
    DateOnly? nextAdjustmentDate,
    string? penaltyNotes,
    string? discountNotes,
    bool generatePaymentsAutomatically,
    string? notes,
    string propertySearchText,
    string residentSearchText,
    DateTimeOffset changedAt,
    UserId? changedByUserId)
  {
    ArgumentNullException.ThrowIfNull(monthlyRent);

    var normalizedResidents = NormalizeResidentIds(residentIds).ToArray();
    if (!normalizedResidents.Contains(primaryResidentId))
    {
      throw new ArgumentException("Primary resident must be included in the contract residents.", nameof(primaryResidentId));
    }

    if (endDate.HasValue && endDate.Value < startDate)
    {
      throw new ArgumentException("End date cannot be before start date.", nameof(endDate));
    }

    if (monthlyRent.Amount < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(monthlyRent), "Monthly rent cannot be negative.");
    }

    if (depositAmount is not null && depositAmount.Amount < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(depositAmount), "Deposit cannot be negative.");
    }

    PrimaryResidentId = primaryResidentId;
    StartDate = startDate;
    EndDate = endDate;
    MonthlyRent = monthlyRent;
    DueDay = RequireDueDay(dueDay);
    DepositAmount = depositAmount;
    AdjustmentIndex = RequireAdjustmentIndex(adjustmentIndex);
    AdjustmentIntervalMonths = RequireAdjustmentInterval(adjustmentIntervalMonths);
    NextAdjustmentDate = nextAdjustmentDate;
    PenaltyNotes = ContractCode.Optional(penaltyNotes, 1000, nameof(penaltyNotes));
    DiscountNotes = ContractCode.Optional(discountNotes, 1000, nameof(discountNotes));
    GeneratePaymentsAutomatically = generatePaymentsAutomatically;
    Notes = ContractCode.Optional(notes, 2000, nameof(notes));
    ReplaceResidents(normalizedResidents, changedAt, changedByUserId);
    SearchText = BuildSearchText(propertySearchText, residentSearchText);
  }

  private void ReplaceResidents(
    IReadOnlyCollection<EntityId> residentIds,
    DateTimeOffset changedAt,
    UserId? changedByUserId)
  {
    residents.Clear();
    foreach (var residentId in residentIds.OrderBy(id => id.Value))
    {
      residents.Add(ContractResident.Create(
        EntityId.New(),
        OrganizationId,
        Id,
        residentId,
        residentId == PrimaryResidentId,
        changedAt,
        changedByUserId));
    }
  }

  private string BuildSearchText(string propertySearchText, string residentSearchText) =>
    ContractCode.NormalizeSearchText(
      PropertyId.Value.ToString("D"),
      PrimaryResidentId.Value.ToString("D"),
      ContractCatalogHint(Status),
      StartDate.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
      EndDate?.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
      propertySearchText,
      residentSearchText,
      Notes,
      PenaltyNotes,
      DiscountNotes);

  private static IEnumerable<EntityId> NormalizeResidentIds(IEnumerable<EntityId> residentIds)
  {
    ArgumentNullException.ThrowIfNull(residentIds);

    var seen = new HashSet<EntityId>();
    foreach (var residentId in residentIds)
    {
      if (seen.Add(residentId))
      {
        yield return residentId;
      }
    }

    if (seen.Count == 0)
    {
      throw new ArgumentException("At least one resident is required.", nameof(residentIds));
    }
  }

  private void EnsureCanMutateTerms()
  {
    if (Status is ContractStatus.Archived or ContractStatus.Cancelled or ContractStatus.Terminated)
    {
      throw new InvalidOperationException("Contract terms cannot be changed from the current status.");
    }
  }

  private static int RequireDueDay(int dueDay) =>
    dueDay is < 1 or > 31
      ? throw new ArgumentOutOfRangeException(nameof(dueDay), "Due day must be between 1 and 31.")
      : dueDay;

  private static int RequireAdjustmentInterval(int intervalMonths) =>
    intervalMonths < 0
      ? throw new ArgumentOutOfRangeException(nameof(intervalMonths), "Adjustment interval cannot be negative.")
      : intervalMonths;

  private static ContractAdjustmentIndex RequireAdjustmentIndex(ContractAdjustmentIndex index) =>
    index == ContractAdjustmentIndex.None
      ? throw new ArgumentException("Adjustment index is required.", nameof(index))
      : index;

  private static string ContractCatalogHint(ContractStatus status) => status.ToString();
}
