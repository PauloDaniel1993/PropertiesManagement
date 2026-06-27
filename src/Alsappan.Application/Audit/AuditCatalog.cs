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
      ["property.archived"] = new("Imovel arquivado", "Property archived"),
      ["property.restored"] = new("Imovel restaurado", "Property restored"),
      ["resident.created"] = new("Morador criado", "Resident created"),
      ["resident.updated"] = new("Morador atualizado", "Resident updated"),
      ["resident.archived"] = new("Morador arquivado", "Resident archived"),
      ["resident.restored"] = new("Morador restaurado", "Resident restored"),
      ["identity.login"] = new("Login realizado", "Login completed"),
      ["identity.login-failed"] = new("Falha de login", "Login failed"),
      ["identity.logout"] = new("Logout realizado", "Logout completed"),
      ["identity.refresh"] = new("Sessao renovada", "Session refreshed"),
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
