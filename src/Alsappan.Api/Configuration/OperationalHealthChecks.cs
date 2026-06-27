using System.Globalization;
using Alsappan.Api.Errors;
using Alsappan.Application.Common.Configuration;
using Alsappan.Infrastructure.Outbox;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Alsappan.Api.Configuration;

internal sealed class DatabaseHealthCheck : IHealthCheck
{
  private readonly IServiceScopeFactory _scopeFactory;

  public DatabaseHealthCheck(IServiceScopeFactory scopeFactory)
  {
    _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
  }

  public async Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default)
  {
    try
    {
      using var scope = _scopeFactory.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<AlsappanDbContext>();
      var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken)
        .ConfigureAwait(false);

      return canConnect
        ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
        : HealthCheckResult.Unhealthy("PostgreSQL did not accept a connection.");
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      return HealthCheckResult.Unhealthy("PostgreSQL health check failed.", exception);
    }
  }
}

internal sealed class StorageHealthCheck : IHealthCheck
{
  private readonly StorageOptions _storageOptions;

  public StorageHealthCheck(IOptions<AlsappanOptions> options)
  {
    ArgumentNullException.ThrowIfNull(options);
    _storageOptions = options.Value.Storage;
  }

  public async Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var rootPath = Path.GetFullPath(_storageOptions.LocalPath);
      Directory.CreateDirectory(rootPath);

      var healthDirectory = Path.Combine(rootPath, ".health");
      Directory.CreateDirectory(healthDirectory);

      var probePath = Path.Combine(healthDirectory, $"{Guid.NewGuid():N}.tmp");
      await File.WriteAllTextAsync(probePath, "ok", cancellationToken)
        .ConfigureAwait(false);

      var content = await File.ReadAllTextAsync(probePath, cancellationToken)
        .ConfigureAwait(false);
      File.Delete(probePath);

      return string.Equals(content, "ok", StringComparison.Ordinal)
        ? HealthCheckResult.Healthy("Configured file storage is writable.")
        : HealthCheckResult.Unhealthy("Configured file storage returned unexpected probe content.");
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      return HealthCheckResult.Unhealthy("File storage health check failed.", exception);
    }
  }
}

internal sealed class BackgroundWorkerHealthCheck : IHealthCheck
{
  private readonly IServiceScopeFactory _scopeFactory;

  public BackgroundWorkerHealthCheck(IServiceScopeFactory scopeFactory)
  {
    _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
  }

  public Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default)
  {
    try
    {
      using var scope = _scopeFactory.CreateScope();
      _ = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

      return Task.FromResult(HealthCheckResult.Healthy("Outbox processor dependencies are registered."));
    }
    catch (InvalidOperationException exception)
    {
      return Task.FromResult(HealthCheckResult.Unhealthy("Outbox processor is not ready.", exception));
    }
  }
}

internal sealed class LocalizationHealthCheck : IHealthCheck
{
  private readonly LocalizationOptions _localizationOptions;
  private readonly ProblemDetailsMessageCatalog _messageCatalog;

  public LocalizationHealthCheck(
    IOptions<AlsappanOptions> options,
    ProblemDetailsMessageCatalog messageCatalog)
  {
    ArgumentNullException.ThrowIfNull(options);
    _localizationOptions = options.Value.Localization;
    _messageCatalog = messageCatalog ?? throw new ArgumentNullException(nameof(messageCatalog));
  }

  public Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (_localizationOptions.SupportedCultures.Count == 0)
    {
      return Task.FromResult(HealthCheckResult.Unhealthy("No supported cultures are configured."));
    }

    if (!_localizationOptions.SupportedCultures.Contains(
      _localizationOptions.DefaultCulture,
      StringComparer.OrdinalIgnoreCase))
    {
      return Task.FromResult(HealthCheckResult.Unhealthy("The default culture is not listed as supported."));
    }

    foreach (var cultureName in _localizationOptions.SupportedCultures)
    {
      try
      {
        _ = CultureInfo.GetCultureInfo(cultureName);
        _ = _messageCatalog.Resolve(ApiProblemCode.Validation, cultureName);
      }
      catch (CultureNotFoundException exception)
      {
        return Task.FromResult(HealthCheckResult.Unhealthy($"Culture '{cultureName}' is not valid.", exception));
      }
      catch (InvalidOperationException exception)
      {
        return Task.FromResult(HealthCheckResult.Unhealthy($"Localization resources failed for '{cultureName}'.", exception));
      }
      catch (ArgumentException exception)
      {
        return Task.FromResult(HealthCheckResult.Unhealthy($"Localization resources failed for '{cultureName}'.", exception));
      }
    }

    return Task.FromResult(HealthCheckResult.Healthy("Localization options and API message resources are loadable."));
  }
}
