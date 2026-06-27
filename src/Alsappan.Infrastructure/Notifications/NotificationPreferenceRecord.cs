using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Infrastructure.Notifications;

public sealed class NotificationPreferenceRecord : IOrganizationScoped
{
  private NotificationPreferenceRecord()
  {
  }

  private NotificationPreferenceRecord(
    OrganizationId organizationId,
    UserId userId,
    string category,
    string channel,
    bool isEnabled,
    bool isMandatory,
    DateTimeOffset createdAt)
  {
    Id = Guid.NewGuid();
    OrganizationId = organizationId;
    UserId = userId;
    Category = category;
    Channel = channel;
    IsEnabled = isMandatory || isEnabled;
    IsMandatory = isMandatory;
    CreatedAt = createdAt;
  }

  public Guid Id { get; private set; }

  public OrganizationId OrganizationId { get; private set; }

  public UserId UserId { get; private set; }

  public string Category { get; private set; } = string.Empty;

  public string Channel { get; private set; } = string.Empty;

  public bool IsEnabled { get; private set; }

  public bool IsMandatory { get; private set; }

  public DateTimeOffset CreatedAt { get; private set; }

  public DateTimeOffset? UpdatedAt { get; private set; }

  public static NotificationPreferenceRecord Create(
    OrganizationId organizationId,
    UserId userId,
    string category,
    string channel,
    bool isEnabled,
    bool isMandatory,
    DateTimeOffset createdAt)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(category);
    ArgumentException.ThrowIfNullOrWhiteSpace(channel);

    if (createdAt == default)
    {
      throw new ArgumentException("Created timestamp is required.", nameof(createdAt));
    }

    return new NotificationPreferenceRecord(
      organizationId,
      userId,
      category,
      channel,
      isEnabled,
      isMandatory,
      createdAt);
  }

  public void Apply(bool isEnabled, bool isMandatory, DateTimeOffset updatedAt)
  {
    if (updatedAt == default)
    {
      throw new ArgumentException("Updated timestamp is required.", nameof(updatedAt));
    }

    IsMandatory = isMandatory;
    IsEnabled = isMandatory || isEnabled;
    UpdatedAt = updatedAt;
  }
}
