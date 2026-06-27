using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Residents;

namespace Alsappan.Domain.Tests.Residents;

public sealed class ResidentTests
{
  [Fact]
  public void CreateNormalizesSearchTextAndDuplicateFields()
  {
    var resident = CreateResident("Joao da Silva");

    Assert.Equal("Joao da Silva", resident.FullName);
    Assert.Equal("joao@example.com", resident.NormalizedEmail);
    Assert.Equal("11999998888", resident.NormalizedPhone);
    Assert.Equal("12345678900", resident.NormalizedDocumentIdentifier);
    Assert.Contains("JOAO DA SILVA", resident.SearchText, StringComparison.Ordinal);
    Assert.Contains("123.456.789-00", resident.SearchText, StringComparison.Ordinal);
  }

  [Fact]
  public void UpdateRefreshesSearchTextAndConcurrencyToken()
  {
    var resident = CreateResident("Joao da Silva");
    var originalToken = resident.ConcurrencyToken;
    var now = DateTimeOffset.Parse("2026-06-27T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    resident.Update(
      "Maria Souza",
      "Maria",
      "maria@example.com",
      "(11) 98888-7777",
      null,
      "CPF",
      "987.654.321-00",
      new DateOnly(1990, 4, 2),
      "Ana Souza",
      "Irma",
      "(11) 97777-6666",
      ResidentStatus.Inactive,
      ResidentPortalStatus.Disabled,
      ResidentPrivacyOptions.ContactData,
      "Contato restrito",
      null,
      now,
      null);

    Assert.NotEqual(originalToken, resident.ConcurrencyToken);
    Assert.Equal(ResidentStatus.Inactive, resident.Status);
    Assert.Equal(ResidentPortalStatus.Disabled, resident.PortalStatus);
    Assert.Contains("MARIA SOUZA", resident.SearchText, StringComparison.Ordinal);
  }

  [Fact]
  public void ArchiveAndRestoreMoveResidentThroughLifecycleStates()
  {
    var resident = CreateResident("Joao da Silva");
    var now = DateTimeOffset.Parse("2026-06-27T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    resident.Archive(now, null);
    Assert.True(resident.IsDeleted);
    Assert.Equal(ResidentStatus.Archived, resident.Status);

    resident.Restore(now.AddMinutes(1), null);
    Assert.False(resident.IsDeleted);
    Assert.Equal(ResidentStatus.Inactive, resident.Status);
  }

  private static Resident CreateResident(string fullName) =>
    Resident.Create(
      EntityId.New(),
      OrganizationId.New(),
      fullName,
      null,
      "JOAO@EXAMPLE.COM",
      "(11) 99999-8888",
      null,
      "CPF",
      "123.456.789-00",
      new DateOnly(1985, 1, 20),
      "Maria",
      "Mae",
      "(11) 98888-7777",
      ResidentStatus.Active,
      ResidentPortalStatus.NotInvited,
      ResidentPrivacyOptions.IdentificationData,
      "Observacoes",
      null,
      DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
