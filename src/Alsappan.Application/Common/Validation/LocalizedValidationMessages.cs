namespace Alsappan.Application.Common.Validation;

public sealed class LocalizedValidationMessages
{
  private readonly Dictionary<string, IReadOnlyDictionary<string, string>> messages =
    new(StringComparer.Ordinal)
    {
      [ValidationMessageKeys.CurrencyCode] = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["pt-BR"] = "Informe uma moeda ISO válida com três letras.",
        ["en-US"] = "Enter a valid three-letter ISO currency."
      },
      [ValidationMessageKeys.DateRange] = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["pt-BR"] = "A data final deve ser igual ou posterior à data inicial.",
        ["en-US"] = "The end date must be the same as or after the start date."
      },
      [ValidationMessageKeys.Email] = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["pt-BR"] = "Informe um e-mail válido.",
        ["en-US"] = "Enter a valid email address."
      },
      [ValidationMessageKeys.InvalidDate] = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["pt-BR"] = "Informe uma data válida no formato AAAA-MM-DD.",
        ["en-US"] = "Enter a valid date in YYYY-MM-DD format."
      },
      [ValidationMessageKeys.InvalidId] = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["pt-BR"] = "Informe um identificador válido.",
        ["en-US"] = "Enter a valid identifier."
      },
      [ValidationMessageKeys.MaxLength] = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["pt-BR"] = "O campo excede o tamanho máximo permitido.",
        ["en-US"] = "The field exceeds the maximum allowed length."
      },
      [ValidationMessageKeys.MinValue] = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["pt-BR"] = "O valor deve ser maior ou igual ao mínimo permitido.",
        ["en-US"] = "The value must be greater than or equal to the minimum allowed."
      },
      [ValidationMessageKeys.Required] = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["pt-BR"] = "Campo obrigatório.",
        ["en-US"] = "Required field."
      }
    };

  public string Resolve(string messageKey, string? locale)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(messageKey);

    var culture = !string.IsNullOrWhiteSpace(locale) &&
      locale.StartsWith("en", StringComparison.OrdinalIgnoreCase)
        ? "en-US"
        : "pt-BR";

    if (messages.TryGetValue(messageKey, out var localizedMessages) &&
      localizedMessages.TryGetValue(culture, out var message))
    {
      return message;
    }

    return messageKey;
  }
}
