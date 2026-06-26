using Alsappan.Application.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Persistence;

public static class AlsappanDbContextOptionsBuilderExtensions
{
  public static DbContextOptionsBuilder UseAlsappanNpgsql(
    this DbContextOptionsBuilder builder,
    DatabaseOptions databaseOptions)
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(databaseOptions);
    ArgumentException.ThrowIfNullOrWhiteSpace(databaseOptions.ConnectionString);

    var schema = AlsappanDbContext.NormalizeSchema(databaseOptions.Schema);

    return builder
      .UseNpgsql(
        databaseOptions.ConnectionString,
        npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", schema))
      .ReplaceService<IModelCacheKeyFactory, AlsappanModelCacheKeyFactory>();
  }

  public static DbContextOptionsBuilder<AlsappanDbContext> UseAlsappanNpgsql(
    this DbContextOptionsBuilder<AlsappanDbContext> builder,
    DatabaseOptions databaseOptions)
  {
    ((DbContextOptionsBuilder)builder).UseAlsappanNpgsql(databaseOptions);
    return builder;
  }
}
