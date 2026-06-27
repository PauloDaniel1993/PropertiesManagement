using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Residents;

namespace Alsappan.Application.Residents;

public static class ResidentCatalog
{
  private static readonly LocalizedCatalogLabel[] StatusLabels =
  [
    new(
      ToStatusCode(ResidentStatus.Active),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Ativo",
        ["en-US"] = "Active"
      }),
    new(
      ToStatusCode(ResidentStatus.Inactive),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Inativo",
        ["en-US"] = "Inactive"
      }),
    new(
      ToStatusCode(ResidentStatus.Archived),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Arquivado",
        ["en-US"] = "Archived"
      })
  ];

  private static readonly LocalizedCatalogLabel[] PortalStatusLabels =
  [
    new(
      ToPortalStatusCode(ResidentPortalStatus.NotInvited),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Nao convidado",
        ["en-US"] = "Not invited"
      }),
    new(
      ToPortalStatusCode(ResidentPortalStatus.Invited),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Convidado",
        ["en-US"] = "Invited"
      }),
    new(
      ToPortalStatusCode(ResidentPortalStatus.Active),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Ativo",
        ["en-US"] = "Active"
      }),
    new(
      ToPortalStatusCode(ResidentPortalStatus.Disabled),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Desabilitado",
        ["en-US"] = "Disabled"
      })
  ];

  private static readonly LocalizedCatalogLabel[] PrivacyFlagLabels =
  [
    new(
      ToPrivacyFlagCode(ResidentPrivacyOptions.ContactData),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Dados de contato",
        ["en-US"] = "Contact data"
      }),
    new(
      ToPrivacyFlagCode(ResidentPrivacyOptions.IdentificationData),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Identificacao",
        ["en-US"] = "Identification"
      }),
    new(
      ToPrivacyFlagCode(ResidentPrivacyOptions.EmergencyContact),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Contato de emergencia",
        ["en-US"] = "Emergency contact"
      }),
    new(
      ToPrivacyFlagCode(ResidentPrivacyOptions.Notes),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Observacoes",
        ["en-US"] = "Notes"
      })
  ];

  public static IReadOnlyList<StatusLabelDto> GetStatusOptions(string? locale = null) =>
    StatusLabels.Select(label => new StatusLabelDto(label.Code, label.GetLabel(locale), ToStatusTone(label.Code)))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetPortalStatusOptions(string? locale = null) =>
    PortalStatusLabels.Select(label => new StatusLabelDto(label.Code, label.GetLabel(locale), ToPortalStatusTone(label.Code)))
      .ToArray();

  public static IReadOnlyList<SelectOptionDto> GetPrivacyFlagOptions(string? locale = null) =>
    PrivacyFlagLabels.Select(label => new SelectOptionDto(label.Code, label.GetLabel(locale))).ToArray();

  public static StatusLabelDto GetStatusLabel(ResidentStatus status, string? locale = null)
  {
    var code = ToStatusCode(status);
    var label = StatusLabels.FirstOrDefault(candidate => candidate.Code == code);
    return new StatusLabelDto(code, label?.GetLabel(locale) ?? code, ToStatusTone(code));
  }

  public static StatusLabelDto GetPortalStatusLabel(ResidentPortalStatus status, string? locale = null)
  {
    var code = ToPortalStatusCode(status);
    var label = PortalStatusLabels.FirstOrDefault(candidate => candidate.Code == code);
    return new StatusLabelDto(code, label?.GetLabel(locale) ?? code, ToPortalStatusTone(code));
  }

  public static string ToStatusCode(ResidentStatus status) =>
    status switch
    {
      ResidentStatus.Active => "active",
      ResidentStatus.Inactive => "inactive",
      ResidentStatus.Archived => "archived",
      _ => string.Empty
    };

  public static string ToPortalStatusCode(ResidentPortalStatus status) =>
    status switch
    {
      ResidentPortalStatus.NotInvited => "not-invited",
      ResidentPortalStatus.Invited => "invited",
      ResidentPortalStatus.Active => "active",
      ResidentPortalStatus.Disabled => "disabled",
      _ => string.Empty
    };

  public static IReadOnlyList<string> ToPrivacyFlagCodes(ResidentPrivacyOptions flags)
  {
    if (flags == ResidentPrivacyOptions.None)
    {
      return [];
    }

    return Enum.GetValues<ResidentPrivacyOptions>()
      .Where(flag => flag != ResidentPrivacyOptions.None && flags.HasFlag(flag))
      .Select(ToPrivacyFlagCode)
      .Where(code => !string.IsNullOrWhiteSpace(code))
      .ToArray();
  }

  public static string ToPrivacyFlagCode(ResidentPrivacyOptions flag) =>
    flag switch
    {
      ResidentPrivacyOptions.ContactData => "contact-data",
      ResidentPrivacyOptions.IdentificationData => "identification-data",
      ResidentPrivacyOptions.EmergencyContact => "emergency-contact",
      ResidentPrivacyOptions.Notes => "notes",
      _ => string.Empty
    };

  public static bool TryParseStatus(string? value, out ResidentStatus status)
  {
    status = ResidentStatus.None;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    status = ResidentCode.NormalizeCode(value) switch
    {
      "active" => ResidentStatus.Active,
      "inactive" => ResidentStatus.Inactive,
      "archived" => ResidentStatus.Archived,
      _ => ResidentStatus.None
    };

    return status != ResidentStatus.None;
  }

  public static bool TryParseMutableStatus(string? value, out ResidentStatus status) =>
    TryParseStatus(value, out status) && status != ResidentStatus.Archived;

  public static bool TryParsePortalStatus(string? value, out ResidentPortalStatus status)
  {
    status = ResidentPortalStatus.None;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    status = ResidentCode.NormalizeCode(value) switch
    {
      "not-invited" => ResidentPortalStatus.NotInvited,
      "invited" => ResidentPortalStatus.Invited,
      "active" => ResidentPortalStatus.Active,
      "disabled" => ResidentPortalStatus.Disabled,
      _ => ResidentPortalStatus.None
    };

    return status != ResidentPortalStatus.None;
  }

  public static bool TryParsePrivacyFlags(IReadOnlyList<string>? values, out ResidentPrivacyOptions flags)
  {
    flags = ResidentPrivacyOptions.None;
    if (values is null || values.Count == 0)
    {
      return true;
    }

    foreach (var value in values)
    {
      var flag = ResidentCode.NormalizeCode(value) switch
      {
        "contact-data" => ResidentPrivacyOptions.ContactData,
        "identification-data" => ResidentPrivacyOptions.IdentificationData,
        "emergency-contact" => ResidentPrivacyOptions.EmergencyContact,
        "notes" => ResidentPrivacyOptions.Notes,
        _ => ResidentPrivacyOptions.None
      };

      if (flag == ResidentPrivacyOptions.None)
      {
        return false;
      }

      flags |= flag;
    }

    return true;
  }

  private static string ToStatusTone(string code) =>
    code switch
    {
      "active" => StatusLabelTones.Success,
      "inactive" => StatusLabelTones.Neutral,
      "archived" => StatusLabelTones.Danger,
      _ => StatusLabelTones.Neutral
    };

  private static string ToPortalStatusTone(string code) =>
    code switch
    {
      "active" => StatusLabelTones.Success,
      "invited" => StatusLabelTones.Info,
      "disabled" => StatusLabelTones.Warning,
      "not-invited" => StatusLabelTones.Neutral,
      _ => StatusLabelTones.Neutral
    };
}
