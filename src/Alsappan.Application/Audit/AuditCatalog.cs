namespace Alsappan.Application.Audit;

public static class AuditCatalog
{
  private static readonly Dictionary<string, AuditCategoryLabels> Categories =
    new Dictionary<string, AuditCategoryLabels>(StringComparer.OrdinalIgnoreCase)
    {
      ["Mutation"] = new("Mutation", "Dados", "Data", "info"),
      ["Security"] = new("Security", "Seguranca", "Security", "danger"),
      ["System"] = new("System", "Sistema", "System", "neutral"),
    };

  private static readonly Dictionary<string, AuditActionLabels> Actions =
    new Dictionary<string, AuditActionLabels>(StringComparer.OrdinalIgnoreCase)
    {
      ["property.created"] = new("Imovel criado", "Property created"),
      ["property.updated"] = new("Imovel atualizado", "Property updated"),
      ["property.status-changed"] = new("Status do imovel alterado", "Property status changed"),
      ["property.status.available"] = new("Imovel disponivel", "Property available"),
      ["property.status.inactive"] = new("Imovel inativo", "Property inactive"),
      ["property.status.maintenance"] = new("Imovel em manutencao", "Property in maintenance"),
      ["property.status.rented"] = new("Imovel alugado", "Property rented"),
      ["property.status.reserved"] = new("Imovel reservado", "Property reserved"),
      ["property.archived"] = new("Imovel arquivado", "Property archived"),
      ["property.restored"] = new("Imovel restaurado", "Property restored"),
      ["resident.created"] = new("Morador criado", "Resident created"),
      ["resident.updated"] = new("Morador atualizado", "Resident updated"),
      ["resident.archived"] = new("Morador arquivado", "Resident archived"),
      ["resident.restored"] = new("Morador restaurado", "Resident restored"),
      ["payment.created"] = new("Pagamento criado", "Payment created"),
      ["payment.updated"] = new("Pagamento atualizado", "Payment updated"),
      ["payment.transaction-recorded"] = new("Recebimento registrado", "Payment transaction recorded"),
      ["payment.transaction-reversed"] = new("Recebimento estornado", "Payment transaction reversed"),
      ["payment.cancelled"] = new("Pagamento cancelado", "Payment cancelled"),
      ["payment.archived"] = new("Pagamento arquivado", "Payment archived"),
      ["payment.restored"] = new("Pagamento restaurado", "Payment restored"),
      ["payment.instruction-created"] = new("Instrucao de pagamento criada", "Payment instruction created"),
      ["payment.provider-event-settled"] = new("Pagamento liquidado pelo provedor", "Payment settled by provider"),
      ["utility-account.created"] = new("Conta de consumo criada", "Utility account created"),
      ["utility-account.updated"] = new("Conta de consumo atualizada", "Utility account updated"),
      ["utility-account.paid"] = new("Conta de consumo paga", "Utility account paid"),
      ["utility-account.cancelled"] = new("Conta de consumo cancelada", "Utility account cancelled"),
      ["utility-account.archived"] = new("Conta de consumo arquivada", "Utility account archived"),
      ["utility-account.restored"] = new("Conta de consumo restaurada", "Utility account restored"),
      ["auth.login.succeeded"] = new("Login realizado", "Login completed"),
      ["auth.login.failed"] = new("Falha de login", "Login failed"),
      ["auth.logout"] = new("Logout realizado", "Logout completed"),
      ["auth.organization.switched"] = new("Organizacao alterada", "Organization switched"),
      ["auth.token.refreshed"] = new("Sessao renovada", "Session refreshed"),
      ["identity.login"] = new("Login realizado", "Login completed"),
      ["identity.login-failed"] = new("Falha de login", "Login failed"),
      ["identity.logout"] = new("Logout realizado", "Logout completed"),
      ["identity.refresh"] = new("Sessao renovada", "Session refreshed"),
      ["administrators.invited"] = new("Administrador convidado", "Administrator invited"),
      ["administrators.role.changed"] = new("Permissoes do administrador alteradas", "Administrator access changed"),
      ["administrators.deactivated"] = new("Administrador desativado", "Administrator deactivated"),
      ["administrators.reactivated"] = new("Administrador reativado", "Administrator reactivated"),
      ["administrators.archived"] = new("Administrador arquivado", "Administrator archived"),
      ["administrator.created"] = new("Administrador criado", "Administrator created"),
      ["administrator.updated"] = new("Administrador atualizado", "Administrator updated"),
      ["administrator.deactivated"] = new("Administrador desativado", "Administrator deactivated"),
      ["administrator.reactivated"] = new("Administrador reativado", "Administrator reactivated"),
      ["administrator.archived"] = new("Administrador arquivado", "Administrator archived"),
    };

  public static IReadOnlyList<AuditCategoryLabelDto> GetCategoryOptions(string? locale = null) =>
    Categories.Values
      .OrderBy(category => category.Code, StringComparer.Ordinal)
      .Select(category => category.ToDto(locale))
      .ToArray();

  public static AuditCategoryLabelDto GetCategoryLabel(string code, string? locale = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);

    return Categories.TryGetValue(code, out var category)
      ? category.ToDto(locale)
      : new AuditCategoryLabelDto(code.Trim(), code.Trim());
  }

  public static string GetActionLabel(string action, string? locale = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(action);

    return Actions.TryGetValue(action, out var label)
      ? label.GetLabel(locale)
      : HumanizeAction(action);
  }

  private static string HumanizeAction(string action)
  {
    var segments = action.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return segments.Length == 0
      ? action.Trim()
      : string.Join(" ", segments.Select(segment => segment.Replace('-', ' ')));
  }

  private sealed record AuditCategoryLabels(
    string Code,
    string PtBrLabel,
    string EnUsLabel,
    string Tone)
  {
    public AuditCategoryLabelDto ToDto(string? locale) =>
      new(Code, IsEnglish(locale) ? EnUsLabel : PtBrLabel, Tone);
  }

  private sealed record AuditActionLabels(string PtBrLabel, string EnUsLabel)
  {
    public string GetLabel(string? locale) => IsEnglish(locale) ? EnUsLabel : PtBrLabel;
  }

  private static bool IsEnglish(string? locale) =>
    locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
}
