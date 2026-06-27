using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Notifications.Repositories;

namespace Alsappan.Application.Notifications;

public static class NotificationCatalog
{
  public const string InAppChannel = "in-app";

  private static readonly string[] Categories =
  [
    "properties",
    "residents",
    "contracts",
    "payments",
    "utility-accounts",
    "documents",
    "pets",
    "vehicles",
    "occurrences",
    "inspections",
    "administrators",
    "settings",
    "system"
  ];

  private static readonly string[] Channels =
  [
    InAppChannel,
    "email",
    "whatsapp"
  ];

  public static IReadOnlyList<SelectOptionDto> GetCategoryOptions(string? locale = null) =>
    Categories
      .Select(category => new SelectOptionDto(category, GetCategoryLabel(category, locale).Label))
      .ToArray();

  public static IReadOnlyList<string> CategoryCodes => Categories;

  public static IReadOnlyList<SelectOptionDto> GetChannelOptions(string? locale = null) =>
    Channels
      .Select(channel => new SelectOptionDto(channel, GetChannelLabel(channel, locale).Label))
      .ToArray();

  public static StatusLabelDto GetCategoryLabel(string category, string? locale = null) =>
    new(NormalizeToken(category), CategoryLabel(NormalizeToken(category), locale), StatusLabelTones.Info);

  public static StatusLabelDto GetChannelLabel(string channel, string? locale = null) =>
    new(NormalizeToken(channel), ChannelLabel(NormalizeToken(channel), locale), StatusLabelTones.Neutral);

  public static StatusLabelDto GetDeliveryStatusLabel(string status, string? locale = null)
  {
    var normalized = NormalizeToken(status);
    var tone = normalized switch
    {
      "delivered" => StatusLabelTones.Success,
      "failed" => StatusLabelTones.Danger,
      "suppressed" => StatusLabelTones.Neutral,
      _ => StatusLabelTones.Warning
    };

    return new StatusLabelDto(normalized, DeliveryStatusLabel(normalized, locale), tone);
  }

  public static StatusLabelDto GetReadStateLabel(bool isRead, string? locale = null) =>
    isRead
      ? new StatusLabelDto("read", IsPortuguese(locale) ? "Lida" : "Read", StatusLabelTones.Neutral)
      : new StatusLabelDto("unread", IsPortuguese(locale) ? "Nao lida" : "Unread", StatusLabelTones.Info);

  public static IReadOnlyList<NotificationPreferenceDto> GetDefaultPreferences(string? locale = null) =>
    Categories
      .SelectMany(category => Channels.Select(channel => new NotificationPreferenceDto(
        category,
        GetCategoryLabel(category, locale).Label,
        channel,
        GetChannelLabel(channel, locale).Label,
        IsEnabled: string.Equals(channel, InAppChannel, StringComparison.OrdinalIgnoreCase),
        IsMandatory:
          string.Equals(category, "system", StringComparison.OrdinalIgnoreCase) &&
          string.Equals(channel, InAppChannel, StringComparison.OrdinalIgnoreCase))))
      .ToArray();

  public static bool IsKnownCategory(string? category) =>
    string.IsNullOrWhiteSpace(category) ||
      Categories.Contains(NormalizeToken(category), StringComparer.OrdinalIgnoreCase);

  public static bool IsKnownChannel(string? channel) =>
    string.IsNullOrWhiteSpace(channel) ||
      Channels.Contains(NormalizeToken(channel), StringComparer.OrdinalIgnoreCase);

  public static string? GetReadPermissionForCategory(string? category) =>
    NormalizeToken(category) switch
    {
      "properties" => PermissionCodes.Read(PermissionModules.Properties),
      "residents" => PermissionCodes.Read(PermissionModules.Residents),
      "contracts" => PermissionCodes.Read(PermissionModules.Contracts),
      "payments" => PermissionCodes.Read(PermissionModules.Payments),
      "utility-accounts" or "utilityaccounts" => PermissionCodes.Read(PermissionModules.UtilityAccounts),
      "documents" => PermissionCodes.Read(PermissionModules.Documents),
      "pets" => PermissionCodes.Read(PermissionModules.Pets),
      "vehicles" => PermissionCodes.Read(PermissionModules.Vehicles),
      "occurrences" => PermissionCodes.Read(PermissionModules.Occurrences),
      "inspections" => PermissionCodes.Read(PermissionModules.Inspections),
      "administrators" => PermissionCodes.Read(PermissionModules.Administrators),
      "settings" => PermissionCodes.Read(PermissionModules.Settings),
      "system" => PermissionCodes.Read(PermissionModules.Notifications),
      _ => null
    };

  public static NotificationListItemDto ToDto(
    NotificationRecordSnapshot notification,
    string? locale = null)
  {
    ArgumentNullException.ThrowIfNull(notification);

    return new NotificationListItemDto(
      notification.Id,
      notification.EventId,
      notification.RecipientUserId,
      GetCategoryLabel(notification.Category, locale),
      notification.EventName,
      EventTypeLabel(notification.EventName, locale),
      RenderTitle(notification, locale),
      RenderMessage(notification, locale),
      BuildDeepLink(notification),
      GetChannelLabel(notification.Channel, locale),
      GetDeliveryStatusLabel(notification.DeliveryStatus, locale),
      GetReadStateLabel(notification.IsRead, locale),
      notification.Payload,
      notification.SubjectEntityType,
      notification.SubjectEntityId,
      notification.SubjectDisplayName,
      notification.OccurredAt,
      notification.CreatedAt,
      notification.IsRead,
      notification.ReadAt,
      notification.IsArchived,
      notification.ArchivedAt,
      notification.CorrelationId);
  }

#pragma warning disable CA1308
  public static string NormalizeToken(string? value) =>
    string.IsNullOrWhiteSpace(value)
      ? string.Empty
      : value.Trim().ToLowerInvariant();
#pragma warning restore CA1308

  private static string RenderTitle(NotificationRecordSnapshot notification, string? locale)
  {
    var portuguese = IsPortuguese(locale);
    var eventName = NormalizeToken(notification.EventName);
    var category = NormalizeToken(notification.Category);

    if (category == "payments" && eventName.Contains("overdue", StringComparison.Ordinal))
    {
      return portuguese ? "Pagamento vencido" : "Payment overdue";
    }

    if (category == "contracts" && eventName.Contains("expir", StringComparison.Ordinal))
    {
      return portuguese ? "Contrato perto do vencimento" : "Contract nearing expiration";
    }

    if (category == "occurrences" && eventName.Contains("assigned", StringComparison.Ordinal))
    {
      return portuguese ? "Ocorrencia atribuida" : "Occurrence assigned";
    }

    if (category == "inspections" && eventName.Contains("scheduled", StringComparison.Ordinal))
    {
      return portuguese ? "Vistoria agendada" : "Inspection scheduled";
    }

    if (category == "documents" && eventName.Contains("request", StringComparison.Ordinal))
    {
      return portuguese ? "Documento solicitado" : "Document requested";
    }

    return EventTypeLabel(notification.EventName, locale);
  }

  private static string RenderMessage(NotificationRecordSnapshot notification, string? locale)
  {
    var portuguese = IsPortuguese(locale);
    var subject = string.IsNullOrWhiteSpace(notification.SubjectDisplayName)
      ? CategoryLabel(notification.Category, locale)
      : notification.SubjectDisplayName!;
    var eventName = NormalizeToken(notification.EventName);
    var category = NormalizeToken(notification.Category);

    if (category == "payments" && eventName.Contains("overdue", StringComparison.Ordinal))
    {
      return portuguese
        ? $"{subject} possui pagamento vencido."
        : $"{subject} has an overdue payment.";
    }

    if (category == "contracts" && eventName.Contains("expir", StringComparison.Ordinal))
    {
      return portuguese
        ? $"{subject} esta perto do vencimento."
        : $"{subject} is nearing expiration.";
    }

    if (category == "occurrences" && eventName.Contains("assigned", StringComparison.Ordinal))
    {
      return portuguese
        ? $"{subject} foi atribuida para acompanhamento."
        : $"{subject} was assigned for follow-up.";
    }

    if (category == "inspections" && eventName.Contains("scheduled", StringComparison.Ordinal))
    {
      return portuguese ? $"{subject} foi agendada." : $"{subject} was scheduled.";
    }

    if (category == "documents" && eventName.Contains("request", StringComparison.Ordinal))
    {
      return portuguese
        ? $"{subject} requer envio ou revisao de documento."
        : $"{subject} requires a document upload or review.";
    }

    if (eventName.EndsWith(".created", StringComparison.Ordinal))
    {
      return portuguese ? $"{subject} foi criado." : $"{subject} was created.";
    }

    if (eventName.EndsWith(".updated", StringComparison.Ordinal))
    {
      return portuguese ? $"{subject} foi atualizado." : $"{subject} was updated.";
    }

    if (eventName.EndsWith(".archived", StringComparison.Ordinal))
    {
      return portuguese ? $"{subject} foi arquivado." : $"{subject} was archived.";
    }

    if (eventName.EndsWith(".restored", StringComparison.Ordinal))
    {
      return portuguese ? $"{subject} foi restaurado." : $"{subject} was restored.";
    }

    return portuguese
      ? $"{subject} recebeu uma atualizacao."
      : $"{subject} received an update.";
  }

  private static string EventTypeLabel(string eventName, string? locale)
  {
    var portuguese = IsPortuguese(locale);
    var normalized = NormalizeToken(eventName);

    return normalized switch
    {
      "property.created" => portuguese ? "Imovel criado" : "Property created",
      "property.updated" => portuguese ? "Imovel atualizado" : "Property updated",
      "property.archived" => portuguese ? "Imovel arquivado" : "Property archived",
      "property.restored" => portuguese ? "Imovel restaurado" : "Property restored",
      "resident.created" => portuguese ? "Morador criado" : "Resident created",
      "resident.updated" => portuguese ? "Morador atualizado" : "Resident updated",
      "resident.archived" => portuguese ? "Morador arquivado" : "Resident archived",
      "resident.restored" => portuguese ? "Morador restaurado" : "Resident restored",
      "payment.created" => portuguese ? "Pagamento criado" : "Payment created",
      "payment.updated" => portuguese ? "Pagamento atualizado" : "Payment updated",
      "payment.transaction-recorded" => portuguese ? "Recebimento registrado" : "Payment transaction recorded",
      "payment.transaction-reversed" => portuguese ? "Recebimento estornado" : "Payment transaction reversed",
      "payment.cancelled" => portuguese ? "Pagamento cancelado" : "Payment cancelled",
      "payment.archived" => portuguese ? "Pagamento arquivado" : "Payment archived",
      "payment.restored" => portuguese ? "Pagamento restaurado" : "Payment restored",
      "payment.instruction-created" => portuguese ? "Instrucao de pagamento criada" : "Payment instruction created",
      "payment.provider-event-settled" => portuguese ? "Pagamento liquidado pelo provedor" : "Payment settled by provider",
      "utility-account.created" => portuguese ? "Conta de consumo criada" : "Utility account created",
      "utility-account.updated" => portuguese ? "Conta de consumo atualizada" : "Utility account updated",
      "utility-account.paid" => portuguese ? "Conta de consumo paga" : "Utility account paid",
      "utility-account.cancelled" => portuguese ? "Conta de consumo cancelada" : "Utility account cancelled",
      "utility-account.archived" => portuguese ? "Conta de consumo arquivada" : "Utility account archived",
      "utility-account.restored" => portuguese ? "Conta de consumo restaurada" : "Utility account restored",
      "administrators.invited" => portuguese ? "Administrador convidado" : "Administrator invited",
      "administrators.role.changed" => portuguese ? "Permissoes de administrador alteradas" : "Administrator permissions changed",
      "administrators.deactivated" => portuguese ? "Administrador desativado" : "Administrator deactivated",
      "administrators.reactivated" => portuguese ? "Administrador reativado" : "Administrator reactivated",
      "administrators.archived" => portuguese ? "Administrador arquivado" : "Administrator archived",
      _ => HumanizeEventName(eventName)
    };
  }

  private static string? BuildDeepLink(NotificationRecordSnapshot notification)
  {
    if (string.IsNullOrWhiteSpace(notification.SubjectEntityId))
    {
      return CategoryRoute(notification.Category);
    }

    var parameter = NormalizeToken(notification.SubjectEntityType) switch
    {
      "property" or "properties" => "propertyId",
      "resident" or "residents" => "residentId",
      "contract" or "contracts" => "contractId",
      "payment" or "payments" => "paymentId",
      "utility-account" or "utility-accounts" or "utilityaccount" or "utilityaccounts" => "utilityAccountId",
      "document" or "documents" => "documentId",
      "pet" or "pets" => "petId",
      "vehicle" or "vehicles" => "vehicleId",
      "occurrence" or "occurrences" => "occurrenceId",
      "inspection" or "inspections" => "inspectionId",
      "identityuser" or "identity-user" or "administrator" or "administrators" => "administratorId",
      _ => null
    };

    var route = CategoryRoute(notification.Category);
    return parameter is null ? route : $"{route}?{parameter}={Uri.EscapeDataString(notification.SubjectEntityId)}";
  }

  private static string CategoryRoute(string category) =>
    NormalizeToken(category) switch
    {
      "properties" => "/imoveis",
      "residents" => "/moradores",
      "contracts" => "/contratos",
      "payments" => "/pagamentos",
      "utility-accounts" or "utilityaccounts" => "/contas-de-consumo",
      "documents" => "/documentos",
      "pets" => "/pets",
      "vehicles" => "/veiculos",
      "occurrences" => "/ocorrencias",
      "inspections" => "/vistorias",
      "administrators" => "/administradores",
      "settings" => "/configuracoes",
      _ => "/notificacoes"
    };

  private static string HumanizeEventName(string eventName)
  {
    if (string.IsNullOrWhiteSpace(eventName))
    {
      return "Evento";
    }

    var parts = eventName
      .Replace('-', ' ')
      .Replace('_', ' ')
      .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var text = string.Join(" ", parts);

    return text.Length == 0 ? "Evento" : char.ToUpperInvariant(text[0]) + text[1..];
  }

  private static string CategoryLabel(string category, string? locale)
  {
    var portuguese = IsPortuguese(locale);
    return NormalizeToken(category) switch
    {
      "properties" => portuguese ? "Imoveis" : "Properties",
      "residents" => portuguese ? "Moradores" : "Residents",
      "contracts" => portuguese ? "Contratos" : "Contracts",
      "payments" => portuguese ? "Pagamentos" : "Payments",
      "utility-accounts" or "utilityaccounts" => portuguese ? "Contas de consumo" : "Utility accounts",
      "documents" => portuguese ? "Documentos" : "Documents",
      "pets" => "Pets",
      "vehicles" => portuguese ? "Veiculos" : "Vehicles",
      "occurrences" => portuguese ? "Ocorrencias" : "Occurrences",
      "inspections" => portuguese ? "Vistorias" : "Inspections",
      "administrators" => portuguese ? "Administradores" : "Administrators",
      "settings" => portuguese ? "Configuracoes" : "Settings",
      "system" => portuguese ? "Sistema" : "System",
      _ => HumanizeEventName(category)
    };
  }

  private static string ChannelLabel(string channel, string? locale)
  {
    var portuguese = IsPortuguese(locale);
    return NormalizeToken(channel) switch
    {
      InAppChannel => portuguese ? "No aplicativo" : "In app",
      "email" => "Email",
      "whatsapp" => "WhatsApp",
      _ => HumanizeEventName(channel)
    };
  }

  private static string DeliveryStatusLabel(string status, string? locale)
  {
    var portuguese = IsPortuguese(locale);
    return NormalizeToken(status) switch
    {
      "delivered" => portuguese ? "Entregue" : "Delivered",
      "failed" => portuguese ? "Falhou" : "Failed",
      "suppressed" => portuguese ? "Suprimida" : "Suppressed",
      _ => portuguese ? "Pendente" : "Pending"
    };
  }

  private static bool IsPortuguese(string? locale) =>
    string.IsNullOrWhiteSpace(locale) ||
      locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
}
