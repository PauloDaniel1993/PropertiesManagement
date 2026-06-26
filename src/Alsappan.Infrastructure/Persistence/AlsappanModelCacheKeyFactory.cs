using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Persistence;

public sealed class AlsappanModelCacheKeyFactory : IModelCacheKeyFactory
{
  public object Create(DbContext context, bool designTime)
  {
    ArgumentNullException.ThrowIfNull(context);

    return context is AlsappanDbContext alsappanDbContext
      ? (context.GetType(), alsappanDbContext.Schema, designTime)
      : (context.GetType(), designTime);
  }
}
