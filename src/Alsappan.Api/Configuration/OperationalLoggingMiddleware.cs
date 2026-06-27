using System.Diagnostics;
using System.Security.Claims;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Tenancy;
using Microsoft.AspNetCore.Routing;

namespace Alsappan.Api.Configuration;

internal sealed class OperationalLoggingMiddleware
{
  public const string RequestIdHeaderName = "X-Request-ID";

  private static readonly Action<ILogger, string, string?, int, long, Exception?> HttpRequestCompleted =
    LoggerMessage.Define<string, string?, int, long>(
      LogLevel.Information,
      new EventId(1000, nameof(HttpRequestCompleted)),
      "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds} ms.");

  private readonly RequestDelegate _next;
  private readonly ILogger<OperationalLoggingMiddleware> _logger;

  public OperationalLoggingMiddleware(
    RequestDelegate next,
    ILogger<OperationalLoggingMiddleware> logger)
  {
    _next = next ?? throw new ArgumentNullException(nameof(next));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task InvokeAsync(HttpContext httpContext)
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    ApplyRequestId(httpContext);
    var stopwatch = Stopwatch.StartNew();
    var scope = OperationalLogScope.Create(httpContext);

    using (_logger.BeginScope(scope))
    {
      try
      {
        await _next(httpContext).ConfigureAwait(false);
      }
      finally
      {
        stopwatch.Stop();
        HttpRequestCompleted(
          _logger,
          httpContext.Request.Method,
          httpContext.Request.Path.Value,
          httpContext.Response.StatusCode,
          stopwatch.ElapsedMilliseconds,
          null);
      }
    }
  }

  private static void ApplyRequestId(HttpContext httpContext)
  {
    if (httpContext.Request.Headers.TryGetValue(RequestIdHeaderName, out var requestId) &&
      !string.IsNullOrWhiteSpace(requestId.FirstOrDefault()))
    {
      httpContext.TraceIdentifier = requestId.First()!;
    }

    httpContext.Response.Headers[RequestIdHeaderName] = httpContext.TraceIdentifier;
  }
}

internal static class OperationalLogScope
{
  public static IReadOnlyDictionary<string, object?> Create(HttpContext httpContext)
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    var entity = ResolveEntity(httpContext);
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["OrganizationId"] = ResolveOrganizationId(httpContext),
      ["UserId"] = ResolveUserId(httpContext.User),
      ["RequestId"] = httpContext.TraceIdentifier,
      ["Action"] = ResolveAction(httpContext),
      ["EntityType"] = entity.EntityType,
      ["EntityId"] = entity.EntityId
    };
  }

  private static string? ResolveOrganizationId(HttpContext httpContext)
  {
    if (httpContext.Request.Headers.TryGetValue(
      TenancyHeaderNames.ActiveOrganizationId,
      out var organizationId) &&
      !string.IsNullOrWhiteSpace(organizationId.FirstOrDefault()))
    {
      return organizationId.First();
    }

    return httpContext.User.FindFirstValue(AuthClaimTypes.ActiveOrganizationId);
  }

  private static string? ResolveUserId(ClaimsPrincipal principal) =>
    principal.FindFirstValue(AuthClaimTypes.UserId) ??
    principal.FindFirstValue(ClaimTypes.NameIdentifier);

  private static string ResolveAction(HttpContext httpContext)
  {
    var endpointName = httpContext
      .GetEndpoint()
      ?.Metadata
      .GetMetadata<IEndpointNameMetadata>()
      ?.EndpointName;

    return string.IsNullOrWhiteSpace(endpointName)
      ? $"{httpContext.Request.Method} {httpContext.Request.Path}"
      : endpointName;
  }

  private static (string? EntityType, string? EntityId) ResolveEntity(HttpContext httpContext)
  {
    var routeValues = httpContext.Request.RouteValues;
    var pathSegments = httpContext.Request.Path.Value?
      .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    var entityType = pathSegments.Length > 1 && string.Equals(pathSegments[0], "v1", StringComparison.OrdinalIgnoreCase)
      ? pathSegments[1]
      : pathSegments.FirstOrDefault();

    var entityId = routeValues.TryGetValue("id", out var id)
      ? id?.ToString()
      : routeValues
        .Where(value => value.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
        .Select(value => value.Value?.ToString())
        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    return (entityType, entityId);
  }
}
