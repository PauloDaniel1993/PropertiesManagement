using Alsappan.Application.Audit.Repositories;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Audit;

public sealed class AuditService : IAuditService
{
  private readonly IAuditRepository _auditRepository;
  private readonly IPermissionService _permissionService;

  public AuditService(
    IAuditRepository auditRepository,
    IPermissionService permissionService)
  {
    _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
    _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
  }

  public async Task<ApplicationOperationResult<PagedResultDto<AuditEntryDto>>> ListAsync(
    AuditListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var permission = await _permissionService.AuthorizeAsync(
        PermissionCodes.Read(PermissionModules.Audit),
        cancellationToken)
      .ConfigureAwait(false);
    if (!permission.IsGranted)
    {
      return ApplicationOperationResult<PagedResultDto<AuditEntryDto>>.Failed(
        permission.Failure == PermissionEvaluationFailure.Unauthenticated
          ? ApplicationOperationFailure.Unauthorized
          : ApplicationOperationFailure.Forbidden);
    }

    _ = request.ToListFilter();
    var page = await _auditRepository.ListAsync(request, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<PagedResultDto<AuditEntryDto>>.Success(
      new PagedResultDto<AuditEntryDto>(
        page.Items.Select(record => ToDto(record, request.Locale)).ToArray(),
        page.Page,
        page.PageSize,
        page.TotalItems));
  }

  public async Task<ApplicationOperationResult<AuditEntryDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    if (id == Guid.Empty)
    {
      return ApplicationOperationResult<AuditEntryDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var permission = await _permissionService.AuthorizeAsync(
        PermissionCodes.Read(PermissionModules.Audit),
        cancellationToken)
      .ConfigureAwait(false);
    if (!permission.IsGranted)
    {
      return ApplicationOperationResult<AuditEntryDto>.Failed(
        permission.Failure == PermissionEvaluationFailure.Unauthenticated
          ? ApplicationOperationFailure.Unauthorized
          : ApplicationOperationFailure.Forbidden);
    }

    var record = await _auditRepository.GetAsync(id, cancellationToken).ConfigureAwait(false);
    return record is null
      ? ApplicationOperationResult<AuditEntryDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<AuditEntryDto>.Success(ToDto(record, locale));
  }

  public Task<IReadOnlyList<AuditCategoryLabelDto>> GetCategoryOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(AuditCatalog.GetCategoryOptions(locale));
  }

  private static AuditEntryDto ToDto(AuditEntryRecord record, string? locale) =>
    new(
      record.Id,
      record.Action,
      AuditCatalog.GetActionLabel(record.Action, locale),
      AuditCatalog.GetCategoryLabel(record.Category, locale),
      record.OccurredAt,
      record.ActorKind,
      record.ActorUserId,
      string.IsNullOrWhiteSpace(record.ActorDisplayName) ? "-" : record.ActorDisplayName.Trim(),
      record.TargetEntityType,
      record.TargetEntityId,
      string.IsNullOrWhiteSpace(record.TargetDisplayName) ? record.TargetEntityId : record.TargetDisplayName.Trim(),
      record.ChangedFields,
      record.Context,
      record.CorrelationId);
}
