using Alsappan.Application.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Alsappan.Infrastructure.Persistence;

public sealed class AlsappanDesignTimeDbContextFactory :
  IDesignTimeDbContextFactory<AlsappanDbContext>
{
  private const string DefaultConnectionString =
    "Host=localhost;Port=5432;Database=alsappan;Username=alsappan;Password=alsappan_dev_password";

  public AlsappanDbContext CreateDbContext(string[] args)
  {
    ArgumentNullException.ThrowIfNull(args);

    var databaseOptions = new DatabaseOptions
    {
      ConnectionString =
        ArgumentValue(args, "--connection") ??
        Environment.GetEnvironmentVariable("ALSAPPAN_DATABASE_CONNECTION_STRING") ??
        Environment.GetEnvironmentVariable("ALSAPPAN__DATABASE__CONNECTIONSTRING") ??
        DefaultConnectionString,
      Schema =
        ArgumentValue(args, "--schema") ??
        Environment.GetEnvironmentVariable("ALSAPPAN_DATABASE_SCHEMA") ??
        Environment.GetEnvironmentVariable("ALSAPPAN__DATABASE__SCHEMA") ??
        "app",
    };

    var options = new DbContextOptionsBuilder<AlsappanDbContext>()
      .UseAlsappanNpgsql(databaseOptions)
      .Options;

    return new AlsappanDbContext(
      options,
      NoActiveOrganizationContext.Instance,
      databaseOptions);
  }

  private static string? ArgumentValue(string[] args, string name)
  {
    for (var index = 0; index < args.Length; index++)
    {
      if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase) &&
        index + 1 < args.Length)
      {
        return args[index + 1];
      }

      var prefix = name + "=";
      if (args[index].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
      {
        return args[index][prefix.Length..];
      }
    }

    return null;
  }
}
