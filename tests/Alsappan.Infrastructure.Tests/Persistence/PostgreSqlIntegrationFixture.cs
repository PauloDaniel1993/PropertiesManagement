using Alsappan.Application.Common.Configuration;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Tests.Persistence;

internal sealed class PostgreSqlIntegrationFixture
{
  public const string ConnectionStringEnvironmentVariable = "ALSAPPAN_TEST_POSTGRES";

  public string? ConnectionString { get; } =
    Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

  public bool IsEnabled => !string.IsNullOrWhiteSpace(ConnectionString);

  public AlsappanDbContext CreateContext(OrganizationId organizationId)
  {
    if (!IsEnabled)
    {
      throw new InvalidOperationException(
        $"Set {ConnectionStringEnvironmentVariable} to enable PostgreSQL integration tests.");
    }

    var databaseOptions = new DatabaseOptions
    {
      ConnectionString = ConnectionString!,
      Schema = "app",
    };

    var options = new DbContextOptionsBuilder<AlsappanDbContext>()
      .UseAlsappanNpgsql(databaseOptions)
      .Options;

    return new AlsappanDbContext(
      options,
      new StaticActiveOrganizationContext(organizationId),
      databaseOptions);
  }
}
