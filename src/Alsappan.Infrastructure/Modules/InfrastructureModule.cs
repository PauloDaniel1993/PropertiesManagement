using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Modules;

internal interface IInfrastructureModule
{
  int Order { get; }

  void AddServices(IServiceCollection services);
}

internal static class InfrastructureModuleRegistrationExtensions
{
  public static IServiceCollection AddInfrastructureModules(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    foreach (var module in CreateInfrastructureModules())
    {
      module.AddServices(services);
    }

    return services;
  }

  private static IInfrastructureModule[] CreateInfrastructureModules() =>
    typeof(IInfrastructureModule).Assembly
      .GetTypes()
      .Where(type => type is { IsAbstract: false, IsInterface: false } &&
        typeof(IInfrastructureModule).IsAssignableFrom(type))
      .Select(CreateInfrastructureModule)
      .OrderBy(module => module.Order)
      .ThenBy(module => module.GetType().FullName, StringComparer.Ordinal)
      .ToArray();

  private static IInfrastructureModule CreateInfrastructureModule(Type moduleType) =>
    Activator.CreateInstance(moduleType) as IInfrastructureModule ??
    throw new InvalidOperationException(
      $"Infrastructure module '{moduleType.FullName}' must define a parameterless constructor.");
}
