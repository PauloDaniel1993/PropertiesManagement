using Alsappan.Application.Common.Authorization;

namespace Alsappan.Application.Timeline;

public static class TimelineCatalog
{
  private static readonly Dictionary<string, (string Pt, string En)> EventLabels =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["property.created"] = ("Imovel criado", "Property created"),
      ["property.updated"] = ("Imovel atualizado", "Property updated"),
      ["property.archived"] = ("Imovel arquivado", "Property archived"),
      ["property.restored"] = ("Imovel restaurado", "Property restored"),
      ["property.status.available"] = ("Imovel marcado como disponivel", "Property marked available"),
      ["property.status.maintenance"] = ("Imovel marcado para manutencao", "Property marked for maintenance"),
      ["property.status.rented"] = ("Imovel marcado como alugado", "Property marked rented"),
      ["property.status.reserved"] = ("Imovel marcado como reservado", "Property marked reserved"),
      ["property.status.inactive"] = ("Imovel marcado como inativo", "Property marked inactive"),
      ["resident.created"] = ("Morador criado", "Resident created"),
      ["resident.updated"] = ("Morador atualizado", "Resident updated"),
      ["resident.archived"] = ("Morador arquivado", "Resident archived"),
      ["resident.restored"] = ("Morador restaurado", "Resident restored"),
      ["contract.created"] = ("Contrato criado", "Contract created"),
      ["contract.updated"] = ("Contrato atualizado", "Contract updated"),
      ["contract.archived"] = ("Contrato arquivado", "Contract archived"),
      ["payment.created"] = ("Pagamento criado", "Payment created"),
      ["payment.updated"] = ("Pagamento atualizado", "Payment updated"),
      ["payment.received"] = ("Pagamento recebido", "Payment received"),
      ["payment.transaction-recorded"] = ("Recebimento registrado", "Payment transaction recorded"),
      ["payment.transaction-reversed"] = ("Recebimento estornado", "Payment transaction reversed"),
      ["payment.cancelled"] = ("Pagamento cancelado", "Payment cancelled"),
      ["payment.archived"] = ("Pagamento arquivado", "Payment archived"),
      ["payment.restored"] = ("Pagamento restaurado", "Payment restored"),
      ["payment.instruction-created"] = ("Instrucao de pagamento criada", "Payment instruction created"),
      ["payment.provider-event-settled"] = ("Pagamento liquidado pelo provedor", "Payment settled by provider"),
      ["utility-account.created"] = ("Conta de consumo criada", "Utility account created"),
      ["utility-account.updated"] = ("Conta de consumo atualizada", "Utility account updated"),
      ["utility-account.paid"] = ("Conta de consumo paga", "Utility account paid"),
      ["utility-account.cancelled"] = ("Conta de consumo cancelada", "Utility account cancelled"),
      ["utility-account.archived"] = ("Conta de consumo arquivada", "Utility account archived"),
      ["utility-account.restored"] = ("Conta de consumo restaurada", "Utility account restored"),
      ["document.uploaded"] = ("Documento enviado", "Document uploaded"),
      ["document.versioned"] = ("Nova versao de documento", "Document version added"),
      ["document.archived"] = ("Documento arquivado", "Document archived"),
      ["pet.created"] = ("Pet cadastrado", "Pet registered"),
      ["pet.updated"] = ("Pet atualizado", "Pet updated"),
      ["vehicle.created"] = ("Veiculo cadastrado", "Vehicle registered"),
      ["vehicle.updated"] = ("Veiculo atualizado", "Vehicle updated"),
      ["occurrence.created"] = ("Ocorrencia criada", "Occurrence created"),
      ["occurrence.updated"] = ("Ocorrencia atualizada", "Occurrence updated"),
      ["occurrence.resolved"] = ("Ocorrencia resolvida", "Occurrence resolved"),
      ["inspection.created"] = ("Vistoria criada", "Inspection created"),
      ["inspection.completed"] = ("Vistoria concluida", "Inspection completed"),
      ["administrators.invited"] = ("Administrador convidado", "Administrator invited"),
      ["administrators.role.changed"] = ("Permissoes de administrador alteradas", "Administrator permissions changed"),
      ["administrators.deactivated"] = ("Administrador desativado", "Administrator deactivated"),
      ["administrators.reactivated"] = ("Administrador reativado", "Administrator reactivated"),
      ["administrators.archived"] = ("Administrador arquivado", "Administrator archived")
    };

  private static readonly Dictionary<string, (string Pt, string En)> EntityLabels =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["property"] = ("Imovel", "Property"),
      ["resident"] = ("Morador", "Resident"),
      ["contract"] = ("Contrato", "Contract"),
      ["payment"] = ("Pagamento", "Payment"),
      ["utilityAccount"] = ("Conta de consumo", "Utility account"),
      ["document"] = ("Documento", "Document"),
      ["pet"] = ("Pet", "Pet"),
      ["vehicle"] = ("Veiculo", "Vehicle"),
      ["occurrence"] = ("Ocorrencia", "Occurrence"),
      ["inspection"] = ("Vistoria", "Inspection"),
      ["identityUser"] = ("Administrador", "Administrator")
    };

  private static readonly Dictionary<string, string> EntityPermissions =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["property"] = PermissionCodes.Read(PermissionModules.Properties),
      ["resident"] = PermissionCodes.Read(PermissionModules.Residents),
      ["contract"] = PermissionCodes.Read(PermissionModules.Contracts),
      ["payment"] = PermissionCodes.Read(PermissionModules.Payments),
      ["utilityAccount"] = PermissionCodes.Read(PermissionModules.UtilityAccounts),
      ["document"] = PermissionCodes.Read(PermissionModules.Documents),
      ["pet"] = PermissionCodes.Read(PermissionModules.Pets),
      ["vehicle"] = PermissionCodes.Read(PermissionModules.Vehicles),
      ["occurrence"] = PermissionCodes.Read(PermissionModules.Occurrences),
      ["inspection"] = PermissionCodes.Read(PermissionModules.Inspections),
      ["identityUser"] = PermissionCodes.Read(PermissionModules.Administrators)
    };

  public static IReadOnlyCollection<string> KnownEntityTypes => EntityLabels.Keys.ToArray();

  public static string NormalizeEntityType(string entityType)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

    var normalized = entityType.Trim();
    var key = normalized.Replace("_", "-", StringComparison.Ordinal).ToUpperInvariant();

    return key switch
    {
      "PROPERTIES" or "PROPERTY" or "RENTAL-PROPERTY" => "property",
      "RESIDENTS" or "RESIDENT" => "resident",
      "CONTRACTS" or "CONTRACT" => "contract",
      "PAYMENTS" or "PAYMENT" => "payment",
      "UTILITY-ACCOUNT" or "UTILITY-ACCOUNTS" or "UTILITYACCOUNT" or "UTILITYACCOUNTS" => "utilityAccount",
      "DOCUMENTS" or "DOCUMENT" => "document",
      "PETS" or "PET" => "pet",
      "VEHICLES" or "VEHICLE" => "vehicle",
      "OCCURRENCES" or "OCCURRENCE" => "occurrence",
      "INSPECTIONS" or "INSPECTION" => "inspection",
      "IDENTITY-USER" or "IDENTITYUSER" or "ADMINISTRATOR" or "ADMINISTRATORS" => "identityUser",
      _ => normalized
    };
  }

  public static string GetEventLabel(string eventType, string? locale)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

    return EventLabels.TryGetValue(eventType.Trim(), out var labels)
      ? SelectLocalized(labels, locale)
      : Humanize(eventType);
  }

  public static string GetEntityTypeLabel(string entityType, string? locale)
  {
    var normalized = NormalizeEntityType(entityType);

    return EntityLabels.TryGetValue(normalized, out var labels)
      ? SelectLocalized(labels, locale)
      : Humanize(normalized);
  }

  public static string GetActorKindLabel(string actorKind, string? locale)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(actorKind);
    var portuguese = IsPortuguese(locale);

    return actorKind.Trim().ToUpperInvariant() switch
    {
      "USER" => portuguese ? "Usuario" : "User",
      "RESIDENT" => portuguese ? "Morador" : "Resident",
      "SYSTEM" => portuguese ? "Sistema" : "System",
      _ => Humanize(actorKind)
    };
  }

  public static string BuildSummary(
    string eventType,
    string subjectEntityType,
    string? subjectDisplayName,
    string? locale)
  {
    var eventLabel = GetEventLabel(eventType, locale);
    var subject = string.IsNullOrWhiteSpace(subjectDisplayName)
      ? GetEntityTypeLabel(subjectEntityType, locale)
      : subjectDisplayName.Trim();

    return $"{eventLabel}: {subject}";
  }

  public static string? GetReadPermissionForEntityType(string entityType)
  {
    var normalized = NormalizeEntityType(entityType);

    return EntityPermissions.TryGetValue(normalized, out var permission)
      ? permission
      : null;
  }

  public static string? GetEntityRoute(string entityType, string entityId)
  {
    if (string.IsNullOrWhiteSpace(entityId))
    {
      return null;
    }

    var normalized = NormalizeEntityType(entityType);
    var encodedId = Uri.EscapeDataString(entityId.Trim());

    return normalized switch
    {
      "property" => $"/imoveis?propertyId={encodedId}",
      "resident" => $"/moradores?residentId={encodedId}",
      "contract" => $"/contratos?contractId={encodedId}",
      "payment" => $"/pagamentos?paymentId={encodedId}",
      "utilityAccount" => $"/contas-de-consumo?utilityAccountId={encodedId}",
      "document" => $"/documentos?documentId={encodedId}",
      "pet" => $"/pets?petId={encodedId}",
      "vehicle" => $"/veiculos?vehicleId={encodedId}",
      "occurrence" => $"/ocorrencias?occurrenceId={encodedId}",
      "inspection" => $"/vistorias?inspectionId={encodedId}",
      "identityUser" => $"/administradores?administratorId={encodedId}",
      _ => null
    };
  }

  private static string SelectLocalized((string Pt, string En) labels, string? locale) =>
    IsPortuguese(locale) ? labels.Pt : labels.En;

  private static bool IsPortuguese(string? locale) =>
    string.IsNullOrWhiteSpace(locale) ||
    locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);

  private static string Humanize(string value)
  {
    var normalized = value.Trim().Replace('.', ' ').Replace('-', ' ');

    return string.IsNullOrWhiteSpace(normalized)
      ? value
      : char.ToUpperInvariant(normalized[0]) + normalized[1..];
  }
}
