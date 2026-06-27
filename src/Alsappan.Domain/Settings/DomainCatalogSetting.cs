using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Settings;

public sealed class DomainCatalogSetting : TenantScopedEntity<EntityId>
{
  private DomainCatalogSetting()
  {
  }

  private DomainCatalogSetting(
    EntityId id,
    OrganizationId organizationId,
    string catalogType,
    string code,
    string labelPtBr,
    string labelEnUs,
    int sortOrder,
    bool isEnabled,
    bool isSystem,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    CatalogType = SettingsCode.NormalizeCode(catalogType);
    Code = SettingsCode.NormalizeCode(code);
    LabelPtBr = SettingsCode.Required(labelPtBr, nameof(labelPtBr), 120);
    LabelEnUs = SettingsCode.Required(labelEnUs, nameof(labelEnUs), 120);
    SortOrder = sortOrder;
    IsEnabled = isEnabled;
    IsSystem = isSystem;
  }

  public string CatalogType { get; private set; } = string.Empty;

  public string Code { get; private set; } = string.Empty;

  public string LabelPtBr { get; private set; } = string.Empty;

  public string LabelEnUs { get; private set; } = string.Empty;

  public int SortOrder { get; private set; }

  public bool IsEnabled { get; private set; }

  public bool IsSystem { get; private set; }

  public static DomainCatalogSetting Create(
    EntityId id,
    OrganizationId organizationId,
    string catalogType,
    string code,
    string labelPtBr,
    string labelEnUs,
    int sortOrder,
    bool isEnabled,
    bool isSystem,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      catalogType,
      code,
      labelPtBr,
      labelEnUs,
      sortOrder,
      isEnabled,
      isSystem,
      createdAt,
      createdByUserId);

  public void Update(
    string labelPtBr,
    string labelEnUs,
    int sortOrder,
    bool isEnabled,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    LabelPtBr = SettingsCode.Required(labelPtBr, nameof(labelPtBr), 120);
    LabelEnUs = SettingsCode.Required(labelEnUs, nameof(labelEnUs), 120);
    SortOrder = sortOrder;
    IsEnabled = isEnabled;
    MarkUpdated(updatedAt, updatedByUserId);
  }
}
