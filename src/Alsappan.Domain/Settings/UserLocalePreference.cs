using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Settings;

public sealed class UserLocalePreference : TenantScopedEntity<EntityId>
{
  private UserLocalePreference()
  {
  }

  private UserLocalePreference(
    EntityId id,
    OrganizationId organizationId,
    UserId userId,
    string locale,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    UserId = userId;
    Locale = SettingsCode.NormalizeLocale(locale);
  }

  public UserId UserId { get; private set; }

  public string Locale { get; private set; } = "pt-BR";

  public static UserLocalePreference Create(
    EntityId id,
    OrganizationId organizationId,
    UserId userId,
    string locale,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, userId, locale, createdAt, createdByUserId);

  public void UpdateLocale(string locale, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    Locale = SettingsCode.NormalizeLocale(locale);
    MarkUpdated(updatedAt, updatedByUserId);
  }
}
