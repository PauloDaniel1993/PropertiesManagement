namespace Alsappan.Api.Modules;

internal interface IApiEndpointModule
{
  int Order { get; }

  void MapEndpoints(RouteGroupBuilder v1);
}

internal static class ApiEndpointModuleExtensions
{
  public static RouteGroupBuilder MapEndpointModules(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    foreach (var module in CreateEndpointModules())
    {
      module.MapEndpoints(v1);
    }

    return v1;
  }

  private static IApiEndpointModule[] CreateEndpointModules() =>
    typeof(IApiEndpointModule).Assembly
      .GetTypes()
      .Where(type => type is { IsAbstract: false, IsInterface: false } &&
        typeof(IApiEndpointModule).IsAssignableFrom(type))
      .Select(CreateEndpointModule)
      .OrderBy(module => module.Order)
      .ThenBy(module => module.GetType().FullName, StringComparer.Ordinal)
      .ToArray();

  private static IApiEndpointModule CreateEndpointModule(Type moduleType) =>
    Activator.CreateInstance(moduleType) as IApiEndpointModule ??
    throw new InvalidOperationException(
      $"API endpoint module '{moduleType.FullName}' must define a parameterless constructor.");
}
