using Alsappan.Api.Authorization;
using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Payments;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Payments;

#pragma warning disable CA1812
internal sealed class PaymentEndpointModule : IApiEndpointModule
{
  public int Order => 360;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapPaymentEndpoints();
}

internal static class PaymentEndpoints
{
  public static RouteGroupBuilder MapPaymentEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var payments = v1.MapGroup("/payments")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Payments");

    payments.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? status,
          Guid? contractId,
          Guid? propertyId,
          Guid? residentId,
          DateOnly? dueFrom,
          DateOnly? dueTo,
          bool? overdueOnly,
          string? sort,
          bool? includeArchived,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new PaymentListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            status,
            contractId,
            propertyId,
            residentId,
            dueFrom,
            dueTo,
            overdueOnly ?? false,
            sort,
            includeArchived ?? false,
            locale);
          var result = await paymentService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_List")
      .RequirePermission(PermissionCodes.Read(PermissionModules.Payments))
      .WithSummary("Lists payment charges in the active organization.")
      .Produces<PagedResultDto<PaymentListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    payments.MapGet(
        "/status-options",
        async (string? locale, IPaymentService paymentService, CancellationToken cancellationToken) =>
        {
          var options = await paymentService.GetStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Payments_GetStatusOptions")
      .WithSummary("Lists payment status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    payments.MapGet(
        "/method-options",
        async (string? locale, IPaymentService paymentService, CancellationToken cancellationToken) =>
        {
          var options = await paymentService.GetMethodOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Payments_GetMethodOptions")
      .WithSummary("Lists payment method options.")
      .Produces<IReadOnlyList<SelectOptionDto>>();

    payments.MapGet(
        "/reconciliation-status-options",
        async (string? locale, IPaymentService paymentService, CancellationToken cancellationToken) =>
        {
          var options = await paymentService.GetReconciliationStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Payments_GetReconciliationStatusOptions")
      .WithSummary("Lists reconciliation status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    payments.MapGet(
        "/provider-options",
        async (string? locale, IPaymentService paymentService, CancellationToken cancellationToken) =>
        {
          var options = await paymentService.GetProviderOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Payments_GetProviderOptions")
      .WithSummary("Lists mocked payment provider options.")
      .Produces<IReadOnlyList<SelectOptionDto>>();

    payments.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_Get")
      .WithSummary("Gets payment charge details.")
      .Produces<PaymentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    payments.MapPost(
        "",
        async (
          PaymentCreateRequestDto request,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.CreateAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_Create")
      .RequirePermission(PermissionCodes.Write(PermissionModules.Payments))
      .WithSummary("Creates a payment charge.")
      .Produces<PaymentDetailDto>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    payments.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          PaymentUpdateRequestDto request,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.UpdateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_Update")
      .WithSummary("Updates payment charge metadata.")
      .Produces<PaymentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    payments.MapPost(
        "/{id:guid}/transactions",
        async (
          Guid id,
          PaymentTransactionRequestDto request,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.RecordTransactionAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_RecordTransaction")
      .WithSummary("Records a payment transaction against a charge.")
      .Produces<PaymentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    payments.MapPost(
        "/{id:guid}/transactions/reverse",
        async (
          Guid id,
          PaymentTransactionReversalRequestDto request,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.ReverseTransactionAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_ReverseTransaction")
      .WithSummary("Reverses a payment transaction.")
      .Produces<PaymentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    payments.MapPost(
        "/{id:guid}/cancel",
        async (
          Guid id,
          PaymentLifecycleRequestDto request,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.CancelAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_Cancel")
      .WithSummary("Cancels a payment charge.")
      .Produces<PaymentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    payments.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.RestoreAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_Restore")
      .WithSummary("Restores an archived payment charge.")
      .Produces<PaymentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    payments.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_Archive")
      .WithSummary("Archives a payment charge.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    payments.MapPost(
        "/{id:guid}/instructions",
        async (
          Guid id,
          PaymentInstructionRequestDto request,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.CreateInstructionAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_CreateInstruction")
      .RequirePermission(PermissionCodes.Write(PermissionModules.Payments))
      .WithSummary("Creates mocked boleto, Pix, or PayPal payment instructions.")
      .Produces<PaymentInstructionDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    payments.MapPost(
        "/provider-events",
        async (
          PaymentProviderEventRequestDto request,
          string? locale,
          IPaymentService paymentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await paymentService.ApplyProviderEventAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Payments_ApplyProviderEvent")
      .WithSummary("Applies a mocked provider settlement event.")
      .Produces<PaymentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    return v1;
  }
}
