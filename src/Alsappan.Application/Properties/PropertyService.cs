using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Properties.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Properties;

namespace Alsappan.Application.Properties;

public sealed class PropertyService : IPropertyService
{
  private readonly IPropertyRepository propertyRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public PropertyService(
    IPropertyRepository propertyRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider? timeProvider = null)
  {
    this.propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver
      ?? throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<PagedResultDto<PropertyListItemDto>>> ListAsync(
    PropertyListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(
        PermissionCodes.Read(PermissionModules.Properties),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<PropertyListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var page = await propertyRepository.ListAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(property => ToListItem(property, request.Locale)).ToArray();

    return ApplicationOperationResult<PagedResultDto<PropertyListItemDto>>.Success(
      new PagedResultDto<PropertyListItemDto>(items, page.Page, page.PageSize, page.TotalItems));
  }

  public async Task<ApplicationOperationResult<PropertyDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Read(PermissionModules.Properties),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var property = await propertyRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);

    return property is null
      ? ApplicationOperationResult<PropertyDetailDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<PropertyDetailDto>.Success(ToDetail(property, locale));
  }

  public async Task<ApplicationOperationResult<PropertyDetailDto>> CreateAsync(
    PropertyCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(
        PermissionCodes.Write(PermissionModules.Properties),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var validation = ValidateMutationRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Invalid(validation);
    }

    var now = timeProvider.GetUtcNow();
    var property = RentalProperty.Create(
      EntityId.New(),
      context.OrganizationId,
      request.Name,
      ParseType(request.Type),
      request.Description,
      ToAddress(request.Address),
      ParseMutableStatus(request.Status),
      ToMoney(request.SuggestedRent),
      request.GarageSpaceCount,
      request.GarageSpaceIdentifiers,
      request.Notes,
      now,
      context.UserId);

    await propertyRepository.AddAsync(property, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("property.created", property, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PropertyDetailDto>.Success(ToDetail(property, locale));
  }

  public async Task<ApplicationOperationResult<PropertyDetailDto>> UpdateAsync(
    Guid id,
    PropertyUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateMutationRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Write(PermissionModules.Properties),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var property = await propertyRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (property is null)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    property.Update(
      request.Name,
      ParseType(request.Type),
      request.Description,
      ToAddress(request.Address),
      ParseMutableStatus(request.Status),
      ToMoney(request.SuggestedRent),
      request.GarageSpaceCount,
      request.GarageSpaceIdentifiers,
      request.Notes,
      timeProvider.GetUtcNow(),
      context.UserId);

    await propertyRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("property.updated", property, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PropertyDetailDto>.Success(ToDetail(property, locale));
  }

  public async Task<ApplicationOperationResult<PropertyDetailDto>> ChangeStatusAsync(
    Guid id,
    PropertyStatusChangeRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).ToList();
    if (!PropertyCatalog.TryParseMutableStatus(request.Status, out var status))
    {
      validation.Add(new ValidationFailure(nameof(request.Status), "validation.status"));
    }

    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Manage(PermissionModules.Properties),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var property = await propertyRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (property is null)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    property.ChangeStatus(status, timeProvider.GetUtcNow(), context.UserId);

    await propertyRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync(
        $"property.status.{PropertyCatalog.ToStatusCode(status)}",
        property,
        context,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PropertyDetailDto>.Success(ToDetail(property, locale));
  }

  public async Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Archive(PermissionModules.Properties),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var property = await propertyRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (property is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    property.Archive(timeProvider.GetUtcNow(), context.UserId);
    await propertyRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("property.archived", property, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public async Task<ApplicationOperationResult<PropertyDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Archive(PermissionModules.Properties),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var property = await propertyRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);
    if (property is null)
    {
      return ApplicationOperationResult<PropertyDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    property.Restore(timeProvider.GetUtcNow(), context.UserId);
    await propertyRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("property.restored", property, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PropertyDetailDto>.Success(ToDetail(property, locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(PropertyCatalog.GetStatusOptions(locale));
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(PropertyCatalog.GetTypeOptions(locale));
  }

  private async Task<ActiveOrganizationContext?> AuthorizeContextAsync(
    string permissionCode,
    CancellationToken cancellationToken)
  {
    var permission = await permissionService.AuthorizeAsync(permissionCode, cancellationToken)
      .ConfigureAwait(false);
    if (!permission.IsGranted)
    {
      return null;
    }

    var context = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    return context.Succeeded ? context.Context : null;
  }

  private async Task WriteMutationSideEffectsAsync(
    string action,
    RentalProperty property,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    var subject = EntityReference.FromGuid("property", property.Id.Value, property.Name);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["status"] = PropertyCatalog.ToStatusCode(property.Status),
      ["type"] = PropertyCatalog.ToTypeCode(property.Type),
      ["city"] = property.Address.City,
      ["state"] = property.Address.StateCode
    };

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "properties",
      action,
      now,
      actor,
      subject,
      ModuleEventConsumer.Audit | ModuleEventConsumer.Timeline | ModuleEventConsumer.Notifications,
      data);

    await auditWriter.WriteAsync(AuditEntryDraft.FromModuleEvent(envelope), cancellationToken)
      .ConfigureAwait(false);
    await outboxWriter.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
  }

  private static PropertyListItemDto ToListItem(RentalProperty property, string? locale)
  {
    var address = ToAddressDto(property.Address);

    return new PropertyListItemDto(
      property.Id.Value,
      property.Name,
      property.Description,
      PropertyCatalog.ToTypeCode(property.Type),
      PropertyCatalog.GetTypeLabel(property.Type, locale),
      address,
      ToLocation(address),
      PropertyCatalog.GetStatusLabel(property.Status, locale),
      new PropertyMoneyDto(property.SuggestedRent.Amount, property.SuggestedRent.Currency),
      property.GarageSpaceCount,
      property.GarageSpaceIdentifiers,
      ToGarageSummary(property.GarageSpaceCount, locale),
      property.IsDeleted,
      property.CreatedAt,
      property.UpdatedAt);
  }

  private static PropertyDetailDto ToDetail(RentalProperty property, string? locale)
  {
    var listItem = ToListItem(property, locale);

    return new PropertyDetailDto(
      listItem.Id,
      listItem.Name,
      listItem.Description,
      listItem.Type,
      listItem.TypeLabel,
      listItem.Address,
      listItem.Location,
      listItem.Status,
      listItem.SuggestedRent,
      listItem.GarageSpaceCount,
      listItem.GarageSpaceIdentifiers,
      listItem.GarageSummary,
      property.Notes,
      RelationshipSummaries(locale),
      property.CreatedAt,
      property.UpdatedAt,
      property.DeletedAt,
      property.ConcurrencyToken.Value);
  }

  private static IReadOnlyList<PropertyRelationshipSummaryDto> RelationshipSummaries(string? locale)
  {
    var portuguese = string.IsNullOrWhiteSpace(locale) ||
      locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);

    return
    [
      new("contracts", portuguese ? "Contratos" : "Contracts", 0, "/contratos"),
      new("residents", portuguese ? "Moradores" : "Residents", 0, "/moradores"),
      new("payments", portuguese ? "Pagamentos" : "Payments", 0, "/pagamentos"),
      new("utility-accounts", portuguese ? "Contas de consumo" : "Utility accounts", 0, "/contas-de-consumo"),
      new("documents", portuguese ? "Documentos" : "Documents", 0, "/documentos"),
      new("pets", portuguese ? "Pets" : "Pets", 0, "/pets"),
      new("vehicles", portuguese ? "Veiculos" : "Vehicles", 0, "/veiculos"),
      new("occurrences", portuguese ? "Ocorrencias" : "Occurrences", 0, "/ocorrencias"),
      new("inspections", portuguese ? "Vistorias" : "Inspections", 0, "/vistorias"),
      new("timeline", "Timeline", 0, "/timeline"),
      new("audit", portuguese ? "Auditoria" : "Audit", 0, "/auditoria")
    ];
  }

  private static IEnumerable<ValidationFailure> ValidateMutationRequest(PropertyCreateRequestDto request)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
    {
      yield return new ValidationFailure(nameof(request.Name), ValidationMessageKeys.Required);
    }

    if (!PropertyCatalog.TryParseType(request.Type, out _))
    {
      yield return new ValidationFailure(nameof(request.Type), "validation.type");
    }

    if (!PropertyCatalog.TryParseMutableStatus(request.Status, out _))
    {
      yield return new ValidationFailure(nameof(request.Status), "validation.status");
    }

    if (request.Address is null)
    {
      yield return new ValidationFailure(nameof(request.Address), ValidationMessageKeys.Required);
    }
    else
    {
      foreach (var failure in ValidateAddress(request.Address))
      {
        yield return failure;
      }
    }

    if (request.SuggestedRent is null)
    {
      yield return new ValidationFailure(nameof(request.SuggestedRent), ValidationMessageKeys.Required);
    }
    else if (request.SuggestedRent.Amount < 0)
    {
      yield return new ValidationFailure(nameof(request.SuggestedRent.Amount), "validation.money");
    }

    if (request.GarageSpaceCount < 0)
    {
      yield return new ValidationFailure(nameof(request.GarageSpaceCount), "validation.garage");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateMutationRequest(PropertyUpdateRequestDto request) =>
    ValidateMutationRequest(new PropertyCreateRequestDto(
      request.Name,
      request.Type,
      request.Description,
      request.Address,
      request.Status,
      request.SuggestedRent,
      request.GarageSpaceCount,
      request.GarageSpaceIdentifiers,
      request.Notes));

  private static IEnumerable<ValidationFailure> ValidateAddress(PropertyAddressDto address)
  {
    if (string.IsNullOrWhiteSpace(address.StreetLine))
    {
      yield return new ValidationFailure(nameof(address.StreetLine), ValidationMessageKeys.Required);
    }

    if (string.IsNullOrWhiteSpace(address.Number))
    {
      yield return new ValidationFailure(nameof(address.Number), ValidationMessageKeys.Required);
    }

    if (string.IsNullOrWhiteSpace(address.Neighborhood))
    {
      yield return new ValidationFailure(nameof(address.Neighborhood), ValidationMessageKeys.Required);
    }

    if (string.IsNullOrWhiteSpace(address.City))
    {
      yield return new ValidationFailure(nameof(address.City), ValidationMessageKeys.Required);
    }

    if (string.IsNullOrWhiteSpace(address.StateCode))
    {
      yield return new ValidationFailure(nameof(address.StateCode), ValidationMessageKeys.Required);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id)
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(id), ValidationMessageKeys.InvalidId);
    }
  }

  private static Address ToAddress(PropertyAddressDto address) =>
    new(
      address.StreetLine,
      address.Number,
      address.Complement,
      address.Neighborhood,
      address.City,
      address.StateCode,
      address.PostalCode,
      address.CountryCode);

  private static PropertyAddressDto ToAddressDto(Address address) =>
    new(
      address.StreetLine,
      address.Number,
      address.Complement,
      address.Neighborhood,
      address.City,
      address.StateCode,
      address.PostalCode,
      address.CountryCode);

  private static Money ToMoney(PropertyMoneyDto money) => new(money.Amount, money.Currency);

  private static PropertyStatus ParseMutableStatus(string value) =>
    PropertyCatalog.TryParseMutableStatus(value, out var status)
      ? status
      : throw new ArgumentException("Invalid property status.", nameof(value));

  private static PropertyType ParseType(string value) =>
    PropertyCatalog.TryParseType(value, out var type)
      ? type
      : throw new ArgumentException("Invalid property type.", nameof(value));

  private static string ToLocation(PropertyAddressDto address) =>
    $"{address.StreetLine}, {address.Number} - {address.Neighborhood}, {address.City}/{address.StateCode}";

  private static string ToGarageSummary(int garageSpaceCount, string? locale)
  {
    if (garageSpaceCount <= 0)
    {
      return string.IsNullOrWhiteSpace(locale) || locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase)
        ? "Nao possui"
        : "No garage";
    }

    var portuguese = string.IsNullOrWhiteSpace(locale) ||
      locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
    if (portuguese)
    {
      return garageSpaceCount == 1 ? "1 vaga(s)" : $"{garageSpaceCount} vaga(s)";
    }

    return garageSpaceCount == 1 ? "1 space" : $"{garageSpaceCount} spaces";
  }
}
