using System.Text;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Identity;

public sealed class IdentityOrganization :
  Entity<OrganizationId>,
  ICreationAudited,
  IModificationAudited,
  ISoftDeletable
{
  private IdentityOrganization()
  {
  }

  private IdentityOrganization(
    OrganizationId id,
    string slug,
    string name,
    string? displayName,
    DateTimeOffset createdAt,
    UserId? createdByUserId,
    string locale,
    string currency)
    : base(id)
  {
    if (createdAt == default)
    {
      throw new ArgumentException("Created timestamp is required.", nameof(createdAt));
    }

    Slug = NormalizeSlug(slug);
    Name = IdentityCode.Required(name, nameof(name));
    DisplayName = IdentityCode.Optional(displayName) ?? Name;
    Locale = IdentityCode.Optional(locale) ?? IdentityDefaults.DefaultLocale;
    Currency = NormalizeCurrency(currency);
    CreatedAt = createdAt;
    CreatedByUserId = createdByUserId;
  }

  public string Slug { get; private set; } = string.Empty;

  public string Name { get; private set; } = string.Empty;

  public string DisplayName { get; private set; } = string.Empty;

  public string Locale { get; private set; } = IdentityDefaults.DefaultLocale;

  public string Currency { get; private set; } = IdentityDefaults.DefaultCurrency;

  public DateTimeOffset CreatedAt { get; private set; }

  public UserId? CreatedByUserId { get; private set; }

  public DateTimeOffset? UpdatedAt { get; private set; }

  public UserId? UpdatedByUserId { get; private set; }

  public DateTimeOffset? DeletedAt { get; private set; }

  public UserId? DeletedByUserId { get; private set; }

  public bool IsDeleted => DeletedAt.HasValue;

  public static IdentityOrganization Create(
    OrganizationId id,
    string slug,
    string name,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    string? displayName = null,
    string locale = IdentityDefaults.DefaultLocale,
    string currency = IdentityDefaults.DefaultCurrency) =>
    new(id, slug, name, displayName, createdAt, createdByUserId, locale, currency);

  public void Rename(
    string slug,
    string name,
    string? displayName,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    Slug = NormalizeSlug(slug);
    Name = IdentityCode.Required(name, nameof(name));
    DisplayName = IdentityCode.Optional(displayName) ?? Name;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void ChangeLocalization(
    string locale,
    string currency,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    Locale = IdentityCode.Optional(locale) ?? IdentityDefaults.DefaultLocale;
    Currency = NormalizeCurrency(currency);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    DeletedAt = deletedAt == default
      ? throw new ArgumentException("Deleted timestamp is required.", nameof(deletedAt))
      : deletedAt;
    DeletedByUserId = deletedByUserId;
    MarkUpdated(deletedAt, deletedByUserId);
  }

  private static string NormalizeSlug(string slug)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(slug);

    var builder = new StringBuilder(slug.Length);
    var previousWasSeparator = false;

    foreach (var character in slug.Trim())
    {
      if (char.IsLetterOrDigit(character))
      {
        builder.Append(char.ToLowerInvariant(character));
        previousWasSeparator = false;
      }
      else if (!previousWasSeparator)
      {
        builder.Append('-');
        previousWasSeparator = true;
      }
    }

    var normalized = builder.ToString().Trim('-');
    if (normalized.Length == 0)
    {
      throw new ArgumentException("Organization slug must contain letters or digits.", nameof(slug));
    }

    return normalized;
  }

  private static string NormalizeCurrency(string currency)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(currency);

    var normalized = currency.Trim().ToUpperInvariant();
    if (normalized.Length != 3 || normalized.Any(character => !char.IsAsciiLetterUpper(character)))
    {
      throw new ArgumentException("Currency must be a three-letter ISO code.", nameof(currency));
    }

    return normalized;
  }

  private void MarkUpdated(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (updatedAt == default)
    {
      throw new ArgumentException("Updated timestamp is required.", nameof(updatedAt));
    }

    UpdatedAt = updatedAt;
    UpdatedByUserId = updatedByUserId;
    RefreshConcurrencyToken();
  }
}
