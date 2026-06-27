using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Search.Repositories;
using Alsappan.Application.Timeline;

namespace Alsappan.Application.Search;

public sealed class GlobalSearchService : IGlobalSearchService
{
  private const int DefaultLimit = 5;
  private const int MaxLimit = 10;
  private const int MinQueryLength = 2;

  private static readonly string[] SearchableEntityTypes =
  [
    "property",
    "resident",
    "contract",
    "payment",
    "document",
    "occurrence",
    "inspection"
  ];

  private static readonly Dictionary<string, string> GroupRoutes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["property"] = "/imoveis",
      ["resident"] = "/moradores",
      ["contract"] = "/contratos",
      ["payment"] = "/pagamentos",
      ["document"] = "/documentos",
      ["occurrence"] = "/ocorrencias",
      ["inspection"] = "/vistorias"
    };

  private readonly IGlobalSearchRepository globalSearchRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;

  public GlobalSearchService(
    IGlobalSearchRepository globalSearchRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver)
  {
    this.globalSearchRepository = globalSearchRepository ??
      throw new ArgumentNullException(nameof(globalSearchRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
  }

  public async Task<ApplicationOperationResult<GlobalSearchResponseDto>> SearchAsync(
    GlobalSearchRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var normalizedQuery = NormalizeQuery(request.Query);
    if (normalizedQuery.Length < MinQueryLength)
    {
      return ApplicationOperationResult<GlobalSearchResponseDto>.Success(
        new GlobalSearchResponseDto(normalizedQuery, 0, []));
    }

    var context = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    if (!context.Succeeded)
    {
      return ApplicationOperationResult<GlobalSearchResponseDto>.Failed(
        context.Failure == ActiveOrganizationResolutionFailure.Unauthenticated
          ? ApplicationOperationFailure.Unauthorized
          : ApplicationOperationFailure.Forbidden);
    }

    var effectivePermissions = await permissionService.GetEffectivePermissionsAsync(cancellationToken)
      .ConfigureAwait(false);
    var readableEntityTypes = GetReadableEntityTypes(effectivePermissions);
    if (readableEntityTypes is { Count: 0 })
    {
      return ApplicationOperationResult<GlobalSearchResponseDto>.Success(
        new GlobalSearchResponseDto(normalizedQuery, 0, []));
    }

    var records = await globalSearchRepository.SearchAsync(
        context.Context!.OrganizationId,
        new GlobalSearchQuery(
          normalizedQuery,
          Math.Clamp(request.Limit <= 0 ? DefaultLimit : request.Limit, 1, MaxLimit),
          readableEntityTypes),
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<GlobalSearchResponseDto>.Success(
      MapResponse(normalizedQuery, records, request.Locale));
  }

  public Task<GlobalSearchResultContractDto> GetContractAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    return Task.FromResult(
      new GlobalSearchResultContractDto(
        SearchableEntityTypes
          .Select(entityType => new GlobalSearchEntityOptionDto(
            entityType,
            TimelineCatalog.GetEntityTypeLabel(entityType, locale),
            TimelineCatalog.GetReadPermissionForEntityType(entityType) ?? string.Empty))
          .ToArray(),
        GetMatchedFieldOptions(locale)));
  }

  private static GlobalSearchResponseDto MapResponse(
    string query,
    IReadOnlyList<GlobalSearchResultRecord> records,
    string? locale)
  {
    var groups = records
      .GroupBy(record => TimelineCatalog.NormalizeEntityType(record.EntityType), StringComparer.OrdinalIgnoreCase)
      .OrderBy(group => Array.IndexOf(SearchableEntityTypes, group.Key))
      .Select(group =>
      {
        var entityType = group.Key;
        return new GlobalSearchGroupDto(
          entityType,
          TimelineCatalog.GetEntityTypeLabel(entityType, locale),
          GetGroupRoute(entityType, query),
          group
            .OrderByDescending(record => record.SortDate)
            .ThenBy(record => record.Label, StringComparer.CurrentCulture)
            .Select(record => new GlobalSearchResultDto(
              entityType,
              TimelineCatalog.GetEntityTypeLabel(entityType, locale),
              record.Id,
              record.Label,
              record.Summary,
              GetMatchedFieldLabel(record.MatchedFieldKey, locale),
              record.Route))
            .ToArray());
      })
      .Where(group => group.Results.Count > 0)
      .ToArray();

    return new GlobalSearchResponseDto(query, groups.Sum(group => group.Results.Count), groups);
  }

  private static HashSet<string>? GetReadableEntityTypes(IReadOnlySet<string> permissions)
  {
    if (HasPermission(permissions, PermissionCodes.Wildcard))
    {
      return null;
    }

    return SearchableEntityTypes
      .Where(entityType =>
      {
        var permission = TimelineCatalog.GetReadPermissionForEntityType(entityType);
        return permission is not null && HasPermission(permissions, permission);
      })
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
  }

  private static bool HasPermission(IReadOnlySet<string> permissions, string permissionCode) =>
    permissions.Contains(PermissionCodes.Wildcard) ||
    permissions.Contains(PermissionCodes.Normalize(permissionCode));

  private static string NormalizeQuery(string? query) => string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();

  private static string GetGroupRoute(string entityType, string query)
  {
    var route = GroupRoutes.TryGetValue(entityType, out var groupRoute) ? groupRoute : "/dashboard";
    return $"{route}?search={Uri.EscapeDataString(query)}";
  }

  private static IReadOnlyList<SelectOptionDto> GetMatchedFieldOptions(string? locale) =>
  [
    new("name", Label("Nome", "Name", locale)),
    new("address", Label("Endereco", "Address", locale)),
    new("contact", Label("Contato", "Contact", locale)),
    new("document", Label("Documento", "Document", locale)),
    new("status", Label("Status", "Status", locale)),
    new("date", Label("Data", "Date", locale)),
    new("searchText", Label("Conteudo pesquisavel", "Searchable content", locale))
  ];

  private static string GetMatchedFieldLabel(string key, string? locale) =>
    key switch
    {
      "name" => Label("Nome", "Name", locale),
      "address" => Label("Endereco", "Address", locale),
      "contact" => Label("Contato", "Contact", locale),
      "document" => Label("Documento", "Document", locale),
      "status" => Label("Status", "Status", locale),
      "date" => Label("Data", "Date", locale),
      _ => Label("Conteudo pesquisavel", "Searchable content", locale)
    };

  private static string Label(string ptBr, string enUs, string? locale) =>
    string.IsNullOrWhiteSpace(locale) || locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase)
      ? ptBr
      : enUs;
}
