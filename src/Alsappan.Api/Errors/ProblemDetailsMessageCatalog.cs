namespace Alsappan.Api.Errors;

internal sealed class ProblemDetailsMessageCatalog
{
  private readonly Dictionary<string, IReadOnlyDictionary<string, ProblemDetailsMessage>> messages =
    new Dictionary<string, IReadOnlyDictionary<string, ProblemDetailsMessage>>(StringComparer.Ordinal)
    {
      [ApiProblemCode.Validation] = new Dictionary<string, ProblemDetailsMessage>(StringComparer.Ordinal)
      {
        ["pt-BR"] = new("Requisição inválida", "Revise os campos informados e tente novamente."),
        ["en-US"] = new("Invalid request", "Review the submitted fields and try again.")
      },
      [ApiProblemCode.Unauthorized] = new Dictionary<string, ProblemDetailsMessage>(StringComparer.Ordinal)
      {
        ["pt-BR"] = new("Autenticação necessária", "Entre novamente para continuar."),
        ["en-US"] = new("Authentication required", "Sign in again to continue.")
      },
      [ApiProblemCode.Forbidden] = new Dictionary<string, ProblemDetailsMessage>(StringComparer.Ordinal)
      {
        ["pt-BR"] = new("Acesso negado", "Você não tem permissão para executar esta ação."),
        ["en-US"] = new("Access denied", "You do not have permission to perform this action.")
      },
      [ApiProblemCode.NotFound] = new Dictionary<string, ProblemDetailsMessage>(StringComparer.Ordinal)
      {
        ["pt-BR"] = new("Registro não encontrado", "O recurso solicitado não foi encontrado."),
        ["en-US"] = new("Record not found", "The requested resource was not found.")
      },
      [ApiProblemCode.Conflict] = new Dictionary<string, ProblemDetailsMessage>(StringComparer.Ordinal)
      {
        ["pt-BR"] = new("Conflito de dados", "A operação não pode ser concluída no estado atual do registro."),
        ["en-US"] = new("Data conflict", "The operation cannot be completed in the record's current state.")
      },
      [ApiProblemCode.Unexpected] = new Dictionary<string, ProblemDetailsMessage>(StringComparer.Ordinal)
      {
        ["pt-BR"] = new("Erro inesperado", "Ocorreu um erro inesperado. Tente novamente."),
        ["en-US"] = new("Unexpected error", "An unexpected error occurred. Try again.")
      }
    };

  public ProblemDetailsMessage Resolve(string code, string? acceptLanguage)
  {
    var culture = ResolveCulture(acceptLanguage);

    return messages.TryGetValue(code, out var localizedMessages) &&
      localizedMessages.TryGetValue(culture, out var message)
        ? message
        : messages[ApiProblemCode.Unexpected][culture];
  }

  public static string ResolveCulture(string? acceptLanguage)
  {
    if (!string.IsNullOrWhiteSpace(acceptLanguage) &&
      acceptLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase))
    {
      return "en-US";
    }

    return "pt-BR";
  }
}

internal sealed record ProblemDetailsMessage(string Title, string Detail);
