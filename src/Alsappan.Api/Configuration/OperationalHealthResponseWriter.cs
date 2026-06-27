using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Alsappan.Api.Configuration;

internal static class OperationalHealthResponseWriter
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public static Task WriteAsync(HttpContext httpContext, HealthReport report)
  {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(report);

    httpContext.Response.ContentType = "application/json";

    var response = new
    {
      status = report.Status.ToString(),
      durationMilliseconds = report.TotalDuration.TotalMilliseconds,
      checks = report.Entries
        .OrderBy(entry => entry.Key, StringComparer.Ordinal)
        .ToDictionary(
          entry => entry.Key,
          entry => new
          {
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            durationMilliseconds = entry.Value.Duration.TotalMilliseconds,
            error = entry.Value.Exception?.Message,
            tags = entry.Value.Tags.OrderBy(tag => tag, StringComparer.Ordinal).ToArray()
          },
          StringComparer.Ordinal)
    };

    return JsonSerializer.SerializeAsync(
      httpContext.Response.Body,
      response,
      JsonOptions,
      httpContext.RequestAborted);
  }
}
