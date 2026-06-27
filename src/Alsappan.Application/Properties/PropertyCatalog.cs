using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Properties;

namespace Alsappan.Application.Properties;

public static class PropertyCatalog
{
  private static readonly LocalizedCatalogLabel[] StatusLabels =
  [
    new(
      ToStatusCode(PropertyStatus.Available),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Disponivel",
        ["en-US"] = "Available"
      }),
    new(
      ToStatusCode(PropertyStatus.HeldForContract),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Reservado",
        ["en-US"] = "Reserved"
      }),
    new(
      ToStatusCode(PropertyStatus.Rented),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Alugado",
        ["en-US"] = "Rented"
      }),
    new(
      ToStatusCode(PropertyStatus.Maintenance),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Manutencao",
        ["en-US"] = "Maintenance"
      }),
    new(
      ToStatusCode(PropertyStatus.Inactive),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Inativo",
        ["en-US"] = "Inactive"
      }),
    new(
      ToStatusCode(PropertyStatus.Archived),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Arquivado",
        ["en-US"] = "Archived"
      })
  ];

  private static readonly LocalizedCatalogLabel[] TypeLabels =
  [
    new(
      ToTypeCode(PropertyType.Apartment),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Apartamento",
        ["en-US"] = "Apartment"
      }),
    new(
      ToTypeCode(PropertyType.House),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Casa",
        ["en-US"] = "House"
      }),
    new(
      ToTypeCode(PropertyType.CommercialRoom),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Sala comercial",
        ["en-US"] = "Commercial room"
      }),
    new(
      ToTypeCode(PropertyType.Land),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Terreno",
        ["en-US"] = "Land"
      }),
    new(
      ToTypeCode(PropertyType.Other),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Outro",
        ["en-US"] = "Other"
      })
  ];

  public static IReadOnlyList<StatusLabelDto> GetStatusOptions(string? locale = null) =>
    StatusLabels.Select(label => new StatusLabelDto(label.Code, label.GetLabel(locale), ToStatusTone(label.Code)))
      .ToArray();

  public static IReadOnlyList<SelectOptionDto> GetTypeOptions(string? locale = null) =>
    TypeLabels.Select(label => new SelectOptionDto(label.Code, label.GetLabel(locale))).ToArray();

  public static StatusLabelDto GetStatusLabel(PropertyStatus status, string? locale = null)
  {
    var code = ToStatusCode(status);
    var label = StatusLabels.FirstOrDefault(candidate => candidate.Code == code);
    return new StatusLabelDto(code, label?.GetLabel(locale) ?? code, ToStatusTone(code));
  }

  public static string GetTypeLabel(PropertyType type, string? locale = null)
  {
    var code = ToTypeCode(type);
    return TypeLabels.FirstOrDefault(candidate => candidate.Code == code)?.GetLabel(locale) ?? code;
  }

  public static string ToStatusCode(PropertyStatus status) =>
    status switch
    {
      PropertyStatus.Available => "available",
      PropertyStatus.HeldForContract => "reserved",
      PropertyStatus.Rented => "rented",
      PropertyStatus.Maintenance => "maintenance",
      PropertyStatus.Inactive => "inactive",
      PropertyStatus.Archived => "archived",
      _ => string.Empty
    };

  public static string ToTypeCode(PropertyType type) =>
    type switch
    {
      PropertyType.Apartment => "apartment",
      PropertyType.House => "house",
      PropertyType.CommercialRoom => "commercial-room",
      PropertyType.Land => "land",
      PropertyType.Other => "other",
      _ => string.Empty
    };

  public static bool TryParseStatus(string? value, out PropertyStatus status)
  {
    status = PropertyStatus.None;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    status = PropertyCode.NormalizeCode(value) switch
    {
      "available" => PropertyStatus.Available,
      "reserved" => PropertyStatus.HeldForContract,
      "rented" => PropertyStatus.Rented,
      "maintenance" => PropertyStatus.Maintenance,
      "inactive" => PropertyStatus.Inactive,
      "archived" => PropertyStatus.Archived,
      _ => PropertyStatus.None
    };

    return status != PropertyStatus.None;
  }

  public static bool TryParseMutableStatus(string? value, out PropertyStatus status) =>
    TryParseStatus(value, out status) && status != PropertyStatus.Archived;

  public static bool TryParseType(string? value, out PropertyType type)
  {
    type = PropertyType.None;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    type = PropertyCode.NormalizeCode(value) switch
    {
      "apartment" => PropertyType.Apartment,
      "house" => PropertyType.House,
      "commercial-room" => PropertyType.CommercialRoom,
      "land" => PropertyType.Land,
      "other" => PropertyType.Other,
      _ => PropertyType.None
    };

    return type != PropertyType.None;
  }

  private static string ToStatusTone(string code) =>
    code switch
    {
      "available" => StatusLabelTones.Success,
      "reserved" => StatusLabelTones.Info,
      "rented" => StatusLabelTones.Info,
      "maintenance" => StatusLabelTones.Warning,
      "inactive" => StatusLabelTones.Neutral,
      "archived" => StatusLabelTones.Danger,
      _ => StatusLabelTones.Neutral
    };
}
