using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Payments;

namespace Alsappan.Application.Payments;

public static class PaymentCatalog
{
  public const string MockBoletoProvider = "mock-boleto";
  public const string MockPixProvider = "mock-pix";
  public const string MockPayPalProvider = "mock-paypal";

  private static readonly Dictionary<PaymentStatus, (string Code, string Pt, string En, string Tone)> Statuses = new()
  {
    [PaymentStatus.Pending] = ("pending", "Pendente", "Pending", StatusLabelTones.Warning),
    [PaymentStatus.Overdue] = ("overdue", "Em atraso", "Overdue", StatusLabelTones.Danger),
    [PaymentStatus.PartiallyPaid] = ("partially-paid", "Parcialmente pago", "Partially paid", StatusLabelTones.Warning),
    [PaymentStatus.Paid] = ("paid", "Pago", "Paid", StatusLabelTones.Success),
    [PaymentStatus.Cancelled] = ("cancelled", "Cancelado", "Cancelled", "archived"),
    [PaymentStatus.Disputed] = ("disputed", "Em disputa", "Disputed", StatusLabelTones.Danger),
    [PaymentStatus.Archived] = ("archived", "Arquivado", "Archived", "archived")
  };

  private static readonly Dictionary<PaymentMethod, (string Code, string Pt, string En)> Methods = new()
  {
    [PaymentMethod.Cash] = ("cash", "Dinheiro", "Cash"),
    [PaymentMethod.BankTransfer] = ("bank-transfer", "Transferencia bancaria", "Bank transfer"),
    [PaymentMethod.Boleto] = ("boleto", "Boleto", "Boleto"),
    [PaymentMethod.Pix] = ("pix", "Pix", "Pix"),
    [PaymentMethod.PayPal] = ("paypal", "PayPal", "PayPal"),
    [PaymentMethod.Card] = ("card", "Cartao", "Card"),
    [PaymentMethod.Other] = ("other", "Outro", "Other")
  };

  private static readonly Dictionary<PaymentReconciliationStatus, (string Code, string Pt, string En, string Tone)> ReconciliationStatuses = new()
  {
    [PaymentReconciliationStatus.NotRequired] = ("not-required", "Nao requer", "Not required", "neutral"),
    [PaymentReconciliationStatus.Pending] = ("pending", "Pendente", "Pending", StatusLabelTones.Warning),
    [PaymentReconciliationStatus.Matched] = ("matched", "Conciliado", "Matched", StatusLabelTones.Success),
    [PaymentReconciliationStatus.Failed] = ("failed", "Falhou", "Failed", StatusLabelTones.Danger),
    [PaymentReconciliationStatus.ManualReview] = ("manual-review", "Revisao manual", "Manual review", StatusLabelTones.Warning)
  };

  private static readonly Dictionary<string, (string Pt, string En, PaymentMethod Method)> Providers =
    new(StringComparer.Ordinal)
    {
      [MockBoletoProvider] = ("Boleto mock", "Mock boleto", PaymentMethod.Boleto),
      [MockPixProvider] = ("Pix mock", "Mock Pix", PaymentMethod.Pix),
      [MockPayPalProvider] = ("PayPal mock", "Mock PayPal", PaymentMethod.PayPal)
    };

  public static IReadOnlyList<StatusLabelDto> GetStatusOptions(string? locale = null) =>
    Statuses.Select(status => ToStatusLabel(status.Key, locale)).ToArray();

  public static IReadOnlyList<SelectOptionDto> GetMethodOptions(string? locale = null) =>
    Methods
      .Select(method => new SelectOptionDto(method.Value.Code, IsPortuguese(locale) ? method.Value.Pt : method.Value.En))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetReconciliationStatusOptions(string? locale = null) =>
    ReconciliationStatuses.Select(status => ToReconciliationStatusLabel(status.Key, locale)).ToArray();

  public static IReadOnlyList<SelectOptionDto> GetProviderOptions(string? locale = null) =>
    Providers
      .Select(provider => new SelectOptionDto(provider.Key, IsPortuguese(locale) ? provider.Value.Pt : provider.Value.En))
      .ToArray();

  public static bool TryParseStatus(string? value, out PaymentStatus status) =>
    TryParse(value, Statuses, PaymentStatus.Pending, out status);

  public static bool TryParseMethod(string? value, out PaymentMethod method) =>
    TryParse(value, Methods, PaymentMethod.Other, out method);

  public static bool TryParseReconciliationStatus(string? value, out PaymentReconciliationStatus status) =>
    TryParse(value, ReconciliationStatuses, PaymentReconciliationStatus.NotRequired, out status);

  public static bool TryGetProviderMethod(string providerCode, out PaymentMethod method)
  {
    var code = PaymentCode.NormalizeCode(providerCode);
    if (Providers.TryGetValue(code, out var provider))
    {
      method = provider.Method;
      return true;
    }

    method = PaymentMethod.Other;
    return false;
  }

  public static string ToStatusCode(PaymentStatus status) => Statuses[status].Code;

  public static string ToMethodCode(PaymentMethod method) => Methods[method].Code;

  public static string ToReconciliationStatusCode(PaymentReconciliationStatus status) =>
    ReconciliationStatuses[status].Code;

  public static StatusLabelDto ToStatusLabel(PaymentStatus status, string? locale = null)
  {
    var metadata = Statuses[status];
    return new StatusLabelDto(metadata.Code, IsPortuguese(locale) ? metadata.Pt : metadata.En, metadata.Tone);
  }

  public static string ToMethodLabel(PaymentMethod method, string? locale = null)
  {
    var metadata = Methods[method];
    return IsPortuguese(locale) ? metadata.Pt : metadata.En;
  }

  public static StatusLabelDto ToReconciliationStatusLabel(
    PaymentReconciliationStatus status,
    string? locale = null)
  {
    var metadata = ReconciliationStatuses[status];
    return new StatusLabelDto(metadata.Code, IsPortuguese(locale) ? metadata.Pt : metadata.En, metadata.Tone);
  }

  public static bool IsPortuguese(string? locale) =>
    string.IsNullOrWhiteSpace(locale) || locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);

  private static bool TryParse<TEnum>(
    string? value,
    IReadOnlyDictionary<TEnum, (string Code, string Pt, string En)> catalog,
    TEnum fallback,
    out TEnum result)
    where TEnum : struct, Enum =>
    TryParse(value, catalog.Select(pair => (pair.Key, pair.Value.Code)), fallback, out result);

  private static bool TryParse<TEnum>(
    string? value,
    IReadOnlyDictionary<TEnum, (string Code, string Pt, string En, string Tone)> catalog,
    TEnum fallback,
    out TEnum result)
    where TEnum : struct, Enum =>
    TryParse(value, catalog.Select(pair => (pair.Key, pair.Value.Code)), fallback, out result);

  private static bool TryParse<TEnum>(
    string? value,
    IEnumerable<(TEnum Value, string Code)> catalog,
    TEnum fallback,
    out TEnum result)
    where TEnum : struct, Enum
  {
    var code = string.IsNullOrWhiteSpace(value) ? string.Empty : PaymentCode.NormalizeCode(value);
    foreach (var candidate in catalog)
    {
      if (string.Equals(candidate.Code, code, StringComparison.Ordinal))
      {
        result = candidate.Value;
        return true;
      }
    }

    result = fallback;
    return false;
  }
}
