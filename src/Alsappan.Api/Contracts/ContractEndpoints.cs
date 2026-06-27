using Alsappan.Api.OperationResults;
using Alsappan.Api.Modules;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Contracts;

#pragma warning disable CA1812
internal sealed class ContractEndpointModule : IApiEndpointModule
{
  public int Order => 320;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapContractEndpoints();
}

internal static class ContractEndpoints
{
  public static RouteGroupBuilder MapContractEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var contracts = v1.MapGroup("/contracts")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Contracts");

    contracts.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? status,
          Guid? propertyId,
          Guid? residentId,
          DateOnly? startsFrom,
          DateOnly? startsTo,
          DateOnly? endsFrom,
          DateOnly? endsTo,
          bool? endingSoonOnly,
          string? sort,
          bool? includeArchived,
          string? locale,
          IContractService contractService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new ContractListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            status,
            propertyId,
            residentId,
            startsFrom,
            startsTo,
            endsFrom,
            endsTo,
            endingSoonOnly ?? false,
            sort,
            includeArchived ?? false,
            locale);
          var result = await contractService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Contracts_List")
      .WithSummary("Lists lease contracts in the active organization.")
      .Produces<PagedResultDto<ContractListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    contracts.MapGet(
        "/status-options",
        async (
          string? locale,
          IContractService contractService,
          CancellationToken cancellationToken) =>
        {
          var options = await contractService.GetStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Contracts_GetStatusOptions")
      .WithSummary("Lists contract status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    contracts.MapGet(
        "/adjustment-index-options",
        async (
          string? locale,
          IContractService contractService,
          CancellationToken cancellationToken) =>
        {
          var options = await contractService.GetAdjustmentIndexOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Contracts_GetAdjustmentIndexOptions")
      .WithSummary("Lists contract rent adjustment index options.")
      .Produces<IReadOnlyList<SelectOptionDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    contracts.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IContractService contractService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await contractService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Contracts_Get")
      .WithSummary("Gets contract details.")
      .Produces<ContractDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    contracts.MapPost(
        "",
        async (
          ContractCreateRequestDto request,
          string? locale,
          IContractService contractService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await contractService.CreateAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Contracts_Create")
      .WithSummary("Creates a lease contract in the active organization.")
      .Produces<ContractDetailDto>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    contracts.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          ContractUpdateRequestDto request,
          string? locale,
          IContractService contractService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await contractService.UpdateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Contracts_Update")
      .WithSummary("Updates contract terms.")
      .Produces<ContractDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    contracts.MapPost(
        "/{id:guid}/activate",
        async (
          Guid id,
          ContractLifecycleRequestDto request,
          string? locale,
          IContractService contractService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await contractService.ActivateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Contracts_Activate")
      .WithSummary("Activates a contract and marks the property as rented.")
      .Produces<ContractDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    contracts.MapPost(
        "/{id:guid}/terminate",
        async (
          Guid id,
          ContractLifecycleRequestDto request,
          string? locale,
          IContractService contractService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await contractService.TerminateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Contracts_Terminate")
      .WithSummary("Terminates an active contract.")
      .Produces<ContractDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    contracts.MapPost(
        "/{id:guid}/cancel",
        async (
          Guid id,
          ContractLifecycleRequestDto request,
          string? locale,
          IContractService contractService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await contractService.CancelAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Contracts_Cancel")
      .WithSummary("Cancels a contract.")
      .Produces<ContractDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    contracts.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IContractService contractService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await contractService.RestoreAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Contracts_Restore")
      .WithSummary("Restores an archived contract.")
      .Produces<ContractDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    contracts.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IContractService contractService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await contractService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Contracts_Archive")
      .WithSummary("Archives a contract.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
