using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Timeline.Repositories;
using Alsappan.Domain.Common.Events;

namespace Alsappan.Application.Timeline;

public sealed class TimelineService : ITimelineService
{
  private readonly ITimelineRepository timelineRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;

  public TimelineService(
    ITimelineRepository timelineRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver)
  {
    this.timelineRepository = timelineRepository ?? throw new ArgumentNullException(nameof(timelineRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
  }

  public async Task<ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>> ListAsync(
    TimelineListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateListBounds(request.Page, request.PageSize, request.From, request.To).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var readableEntityTypes = await GetReadableEntityTypesAsync(cancellationToken).ConfigureAwait(false);
    if (IsDeniedEntityFilter(request.EntityType, readableEntityTypes) ||
      IsDeniedEntityFilter(request.RelatedEntityType, readableEntityTypes))
    {
      return ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Success(
        EmptyPage(request.Page, request.PageSize));
    }

    var page = await timelineRepository.ListAsync(
        NormalizeRequest(request),
        context.OrganizationId,
        readableEntityTypes,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Success(MapPage(page, request.Locale));
  }

  public async Task<ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>> ListEntityAsync(
    string entityType,
    string entityId,
    TimelineEntityListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
    ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateListBounds(request.Page, request.PageSize, request.From, request.To).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var normalizedEntityType = TimelineCatalog.NormalizeEntityType(entityType);
    var readableEntityTypes = await GetReadableEntityTypesAsync(cancellationToken).ConfigureAwait(false);
    if (IsDeniedEntityFilter(normalizedEntityType, readableEntityTypes) ||
      IsDeniedEntityFilter(request.RelatedEntityType, readableEntityTypes))
    {
      return ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var page = await timelineRepository.ListEntityAsync(
        normalizedEntityType,
        entityId.Trim(),
        NormalizeRequest(request),
        context.OrganizationId,
        readableEntityTypes,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Success(MapPage(page, request.Locale));
  }

  private async Task<ActiveOrganizationContext?> AuthorizeContextAsync(CancellationToken cancellationToken)
  {
    var permission = await permissionService
      .AuthorizeAsync(PermissionCodes.Read(PermissionModules.Timeline), cancellationToken)
      .ConfigureAwait(false);
    if (!permission.IsGranted)
    {
      return null;
    }

    var context = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    return context.Succeeded ? context.Context : null;
  }

  private async Task<IReadOnlySet<string>?> GetReadableEntityTypesAsync(CancellationToken cancellationToken)
  {
    var permissions = await permissionService.GetEffectivePermissionsAsync(cancellationToken)
      .ConfigureAwait(false);
    if (permissions.Contains(PermissionCodes.Wildcard))
    {
      return null;
    }

    var readable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var entityType in TimelineCatalog.KnownEntityTypes)
    {
      var permission = TimelineCatalog.GetReadPermissionForEntityType(entityType);
      if (permission is not null && permissions.Contains(PermissionCodes.Normalize(permission)))
      {
        readable.Add(entityType);
      }
    }

    return readable;
  }

  private static TimelineListRequestDto NormalizeRequest(TimelineListRequestDto request) =>
    request with
    {
      EntityType = string.IsNullOrWhiteSpace(request.EntityType)
        ? null
        : TimelineCatalog.NormalizeEntityType(request.EntityType),
      EventType = Optional(request.EventType),
      RelatedEntityType = string.IsNullOrWhiteSpace(request.RelatedEntityType)
        ? null
        : TimelineCatalog.NormalizeEntityType(request.RelatedEntityType),
      RelatedEntityId = Optional(request.RelatedEntityId),
      Sort = Optional(request.Sort)
    };

  private static TimelineEntityListRequestDto NormalizeRequest(TimelineEntityListRequestDto request) =>
    request with
    {
      EventType = Optional(request.EventType),
      RelatedEntityType = string.IsNullOrWhiteSpace(request.RelatedEntityType)
        ? null
        : TimelineCatalog.NormalizeEntityType(request.RelatedEntityType),
      RelatedEntityId = Optional(request.RelatedEntityId),
      Sort = Optional(request.Sort)
    };

  private static bool IsDeniedEntityFilter(
    string? entityType,
    IReadOnlySet<string>? readableEntityTypes)
  {
    if (string.IsNullOrWhiteSpace(entityType) || readableEntityTypes is null)
    {
      return false;
    }

    return !readableEntityTypes.Contains(TimelineCatalog.NormalizeEntityType(entityType));
  }

  private static PagedResultDto<TimelineEntryDto> MapPage(
    PagedResultDto<TimelineEntryRecord> page,
    string? locale) =>
    new(
      page.Items.Select(entry => ToDto(entry, locale)).ToArray(),
      page.Page,
      page.PageSize,
      page.TotalItems);

  private static TimelineEntryDto ToDto(TimelineEntryRecord entry, string? locale)
  {
    var subject = ToEntityDto(entry.Subject, locale);
    var eventLabel = TimelineCatalog.GetEventLabel(entry.EventType, locale);

    return new TimelineEntryDto(
      entry.Id,
      entry.EventId,
      entry.ModuleName,
      entry.EventType,
      eventLabel,
      entry.OccurredAt,
      new TimelineActorDto(
        entry.Actor.Kind,
        TimelineCatalog.GetActorKindLabel(entry.Actor.Kind, locale),
        entry.Actor.UserId?.Value,
        entry.Actor.DisplayName),
      subject,
      entry.RelatedEntities.Select(related => ToEntityDto(related, locale)).ToArray(),
      entry.Data,
      new TimelineDisplayDto(
        eventLabel,
        TimelineCatalog.BuildSummary(entry.EventType, entry.Subject.EntityType, entry.Subject.DisplayName, locale)),
      entry.CorrelationId,
      subject.Route);
  }

  private static TimelineEntityReferenceDto ToEntityDto(EntityReference reference, string? locale) =>
    new(
      TimelineCatalog.NormalizeEntityType(reference.EntityType),
      TimelineCatalog.GetEntityTypeLabel(reference.EntityType, locale),
      reference.EntityId,
      reference.DisplayName,
      TimelineCatalog.GetEntityRoute(reference.EntityType, reference.EntityId));

  private static PagedResultDto<TimelineEntryDto> EmptyPage(int page, int pageSize) =>
    new([], page, pageSize, 0);

  private static IEnumerable<ValidationFailure> ValidateListBounds(
    int page,
    int pageSize,
    DateOnly? from,
    DateOnly? to)
  {
    if (page < 1)
    {
      yield return new ValidationFailure(nameof(page), "validation.page");
    }

    if (pageSize is < 1 or > ListFilterDto.MaxPageSize)
    {
      yield return new ValidationFailure(nameof(pageSize), "validation.pageSize");
    }

    if (from.HasValue && to.HasValue && to.Value < from.Value)
    {
      yield return new ValidationFailure(nameof(to), "validation.dateRange");
    }
  }

  private static string? Optional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
