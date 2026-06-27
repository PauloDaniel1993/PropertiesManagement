using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Pets;

namespace Alsappan.Application.Pets;

public static class PetCatalog
{
  private static readonly Dictionary<PetSpecies, (string Code, string Pt, string En, string Tone)> Species =
    new()
    {
      [PetSpecies.Dog] = ("dog", "Cachorro", "Dog", StatusLabelTones.Info),
      [PetSpecies.Cat] = ("cat", "Gato", "Cat", StatusLabelTones.Info),
      [PetSpecies.Bird] = ("bird", "Ave", "Bird", StatusLabelTones.Neutral),
      [PetSpecies.Rabbit] = ("rabbit", "Coelho", "Rabbit", StatusLabelTones.Neutral),
      [PetSpecies.Fish] = ("fish", "Peixe", "Fish", StatusLabelTones.Neutral),
      [PetSpecies.Reptile] = ("reptile", "Reptil", "Reptile", StatusLabelTones.Warning),
      [PetSpecies.Other] = ("other", "Outro", "Other", StatusLabelTones.Neutral)
    };

  private static readonly Dictionary<PetAuthorizationStatus, (string Code, string Pt, string En, string Tone)> Statuses =
    new()
    {
      [PetAuthorizationStatus.Pending] = ("pending", "Pendente", "Pending", StatusLabelTones.Warning),
      [PetAuthorizationStatus.Authorized] = ("authorized", "Autorizado", "Authorized", StatusLabelTones.Success),
      [PetAuthorizationStatus.Denied] = ("denied", "Negado", "Denied", StatusLabelTones.Danger),
      [PetAuthorizationStatus.Inactive] = ("inactive", "Inativo", "Inactive", StatusLabelTones.Neutral),
      [PetAuthorizationStatus.Archived] = ("archived", "Arquivado", "Archived", "archived")
    };

  private static readonly Dictionary<PetDocumentKind, (string Code, string Pt, string En, string Tone)> DocumentKinds =
    new()
    {
      [PetDocumentKind.VaccinationRecord] = ("vaccination-record", "Carteira de vacinacao", "Vaccination record", StatusLabelTones.Info),
      [PetDocumentKind.AuthorizationForm] = ("authorization-form", "Formulario de autorizacao", "Authorization form", StatusLabelTones.Success)
    };

  public static IReadOnlyList<StatusLabelDto> GetSpeciesOptions(string? locale = null) =>
    Species
      .OrderBy(species => species.Value.Code, StringComparer.Ordinal)
      .Select(species => ToSpeciesLabel(species.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetAuthorizationStatusOptions(string? locale = null) =>
    Statuses
      .OrderBy(status => status.Value.Code, StringComparer.Ordinal)
      .Select(status => ToAuthorizationStatusLabel(status.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetDocumentKindOptions(string? locale = null) =>
    DocumentKinds
      .OrderBy(kind => kind.Value.Code, StringComparer.Ordinal)
      .Select(kind => ToDocumentKindLabel(kind.Key, locale))
      .ToArray();

  public static StatusLabelDto ToSpeciesLabel(PetSpecies species, string? locale = null)
  {
    var labels = Species[species];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToAuthorizationStatusLabel(
    PetAuthorizationStatus status,
    string? locale = null)
  {
    var labels = Statuses[status];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToDocumentKindLabel(PetDocumentKind kind, string? locale = null)
  {
    var labels = DocumentKinds[kind];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static bool TryParseSpecies(string? code, out PetSpecies species) =>
    TryParse(code, Species, out species);

  public static bool TryParseAuthorizationStatus(
    string? code,
    out PetAuthorizationStatus authorizationStatus) =>
    TryParse(code, Statuses, out authorizationStatus);

  public static bool TryParseDocumentKind(string? code, out PetDocumentKind kind) =>
    TryParse(code, DocumentKinds, out kind);

  public static string GetDocumentKindCode(PetDocumentKind kind) => DocumentKinds[kind].Code;

  private static bool TryParse<TEnum>(
    string? code,
    IReadOnlyDictionary<TEnum, (string Code, string Pt, string En, string Tone)> values,
    out TEnum value)
    where TEnum : struct, Enum
  {
    foreach (var candidate in values)
    {
      if (string.Equals(candidate.Value.Code, code, StringComparison.OrdinalIgnoreCase))
      {
        value = candidate.Key;
        return true;
      }
    }

    value = default;
    return false;
  }

  private static bool IsEnglish(string? locale) =>
    locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
}
