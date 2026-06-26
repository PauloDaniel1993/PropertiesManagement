namespace Alsappan.Api.Contracts;

internal static class ApiConventions
{
  public const string CurrentVersion = "v1";
  public const string VersionPrefix = "/v1";
  public const string TraceIdExtension = "traceId";
  public const string ErrorCodeExtension = "code";

  public static IReadOnlyList<string> SupportedCultures { get; } = ["pt-BR", "en-US"];
}
