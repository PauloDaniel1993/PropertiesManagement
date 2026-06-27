using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Vehicles;

namespace Alsappan.Application.Vehicles;

public static class VehicleCatalog
{
  private static readonly Dictionary<VehicleType, (string Code, string Pt, string En, string Tone)> Types =
    new()
    {
      [VehicleType.Car] = ("car", "Carro", "Car", StatusLabelTones.Info),
      [VehicleType.Motorcycle] = ("motorcycle", "Moto", "Motorcycle", StatusLabelTones.Warning),
      [VehicleType.Truck] = ("truck", "Caminhao", "Truck", StatusLabelTones.Neutral),
      [VehicleType.Van] = ("van", "Van", "Van", StatusLabelTones.Neutral),
      [VehicleType.Bicycle] = ("bicycle", "Bicicleta", "Bicycle", StatusLabelTones.Success),
      [VehicleType.Other] = ("other", "Outro", "Other", StatusLabelTones.Neutral)
    };

  private static readonly Dictionary<VehicleAuthorizationStatus, (string Code, string Pt, string En, string Tone)> Statuses =
    new()
    {
      [VehicleAuthorizationStatus.Pending] = ("pending", "Pendente", "Pending", StatusLabelTones.Warning),
      [VehicleAuthorizationStatus.Authorized] = ("authorized", "Autorizado", "Authorized", StatusLabelTones.Success),
      [VehicleAuthorizationStatus.Denied] = ("denied", "Negado", "Denied", StatusLabelTones.Danger),
      [VehicleAuthorizationStatus.Inactive] = ("inactive", "Inativo", "Inactive", StatusLabelTones.Neutral),
      [VehicleAuthorizationStatus.Archived] = ("archived", "Arquivado", "Archived", "archived")
    };

  public static IReadOnlyList<StatusLabelDto> GetTypeOptions(string? locale = null) =>
    Types
      .OrderBy(type => type.Value.Code, StringComparer.Ordinal)
      .Select(type => ToTypeLabel(type.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetAuthorizationStatusOptions(string? locale = null) =>
    Statuses
      .OrderBy(status => status.Value.Code, StringComparer.Ordinal)
      .Select(status => ToAuthorizationStatusLabel(status.Key, locale))
      .ToArray();

  public static StatusLabelDto ToTypeLabel(VehicleType type, string? locale = null)
  {
    var labels = Types[type];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToAuthorizationStatusLabel(
    VehicleAuthorizationStatus status,
    string? locale = null)
  {
    var labels = Statuses[status];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static bool TryParseType(string? code, out VehicleType type) =>
    TryParse(code, Types, out type);

  public static bool TryParseAuthorizationStatus(
    string? code,
    out VehicleAuthorizationStatus status) =>
    TryParse(code, Statuses, out status);

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
