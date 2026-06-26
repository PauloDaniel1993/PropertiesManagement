using System.Globalization;
using System.Text.Json;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Domain.Tests;

public sealed class ModuleEventEnvelopeTests
{
  private static readonly JsonSerializerOptions CamelCaseJson = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  [Fact]
  public void CreateBuildsTenantScopedEnvelopeForMultipleConsumers()
  {
    var organizationId = OrganizationId.New();
    var actor = EventActor.User(UserId.New(), "Paulo");
    var subject = EntityReference.FromGuid("property", Guid.NewGuid(), "Calabria casa1");

    var envelope = ModuleEventEnvelope.Create(
      organizationId,
      "properties",
      "property.created",
      Timestamp("2026-06-01T10:00:00Z"),
      actor,
      subject,
      ModuleEventConsumer.Audit |
        ModuleEventConsumer.Timeline |
        ModuleEventConsumer.Notifications |
        ModuleEventConsumer.DashboardProjection |
        ModuleEventConsumer.ResidentPortal |
        ModuleEventConsumer.TenantBackgroundProcessing,
      new Dictionary<string, string> { ["status"] = "available" },
      [EntityReference.FromGuid("resident", Guid.NewGuid())],
      "request-1",
      locale: "pt-BR");

    Assert.NotEqual(Guid.Empty, envelope.EventId);
    Assert.Equal(organizationId, envelope.OrganizationId);
    Assert.True(envelope.IsVisibleToResidentPortal);
    Assert.Equal("available", envelope.Data["status"]);
    Assert.Single(envelope.RelatedEntities);
  }

  [Fact]
  public void ConstructorRejectsEnvelopeWithoutConsumer()
  {
    Assert.Throws<ArgumentException>(() =>
      new ModuleEventEnvelope(
        Guid.NewGuid(),
        OrganizationId.New(),
        "properties",
        "property.created",
        DateTimeOffset.UtcNow,
        EventActor.System(),
        EntityReference.FromGuid("property", Guid.NewGuid()),
        ModuleEventConsumer.None));
  }

  [Fact]
  public void EnvelopeSerializesWithCanonicalEventData()
  {
    var envelope = ModuleEventEnvelope.Create(
      OrganizationId.New(),
      "payments",
      "payment.overdue",
      Timestamp("2026-06-01T10:00:00Z"),
      EventActor.System(),
      EntityReference.FromGuid("payment", Guid.NewGuid()),
      ModuleEventConsumer.Audit | ModuleEventConsumer.Notifications,
      new Dictionary<string, string> { ["amount"] = "700.00", ["currency"] = "BRL" });

    var json = JsonSerializer.Serialize(envelope, CamelCaseJson);

    Assert.Contains("\"eventName\":\"payment.overdue\"", json, StringComparison.Ordinal);
    Assert.Contains("\"organizationId\"", json, StringComparison.Ordinal);
    Assert.Contains("\"amount\":\"700.00\"", json, StringComparison.Ordinal);
  }

  private static DateTimeOffset Timestamp(string value) =>
    DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}
