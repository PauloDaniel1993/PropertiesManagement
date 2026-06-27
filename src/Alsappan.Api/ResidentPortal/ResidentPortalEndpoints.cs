using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Documents;
using Alsappan.Application.Payments;
using Alsappan.Application.ResidentPortal;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.ResidentPortal;

#pragma warning disable CA1812
internal sealed class ResidentPortalEndpointModule : IApiEndpointModule
{
  public int Order => 365;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapResidentPortalEndpoints();
}

internal static class ResidentPortalEndpoints
{
  private const long MultipartRequestOverheadBytes = 1024 * 1024;
  private const long MaxMultipartRequestBodyBytes = DocumentCatalog.MaxFileSizeBytes + MultipartRequestOverheadBytes;

  public static RouteGroupBuilder MapResidentPortalEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var portal = v1.MapGroup("/resident-portal")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Resident Portal");

    portal.MapGet(
        "/summary",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.GetSummaryAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_Summary")
      .WithSummary("Gets the resident portal summary.")
      .Produces<ResidentPortalSummaryDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapGet(
        "/profile",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.GetProfileAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_Profile")
      .WithSummary("Gets the linked resident profile.")
      .Produces<ResidentPortalProfileDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapGet(
        "/property",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.GetLinkedPropertyAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_Property")
      .WithSummary("Gets the resident linked property.")
      .Produces<ResidentPortalPropertyDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapGet(
        "/contracts",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.ListContractsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_Contracts")
      .WithSummary("Lists contracts visible to the resident.")
      .Produces<IReadOnlyList<ResidentPortalContractDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapGet(
        "/payments",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.ListPaymentsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_Payments")
      .WithSummary("Lists payments visible to the resident.")
      .Produces<IReadOnlyList<ResidentPortalPaymentDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapPost(
        "/payments/{paymentId:guid}/instructions",
        async (
          Guid paymentId,
          PaymentInstructionRequestDto request,
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.CreatePaymentInstructionAsync(
              paymentId,
              request,
              locale,
              cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_CreatePaymentInstruction")
      .WithSummary("Creates mocked provider payment instructions for a resident-visible payment.")
      .Produces<PaymentInstructionDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    portal.MapGet(
        "/documents",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.ListDocumentsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_Documents")
      .WithSummary("Lists documents visible to the resident.")
      .Produces<IReadOnlyList<ResidentPortalDocumentDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapPost(
        "/documents",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var formResult = await ReadUploadFormAsync(httpContext, cancellationToken).ConfigureAwait(false);
          if (!formResult.Succeeded)
          {
            return ApplicationEndpointResults.FromOperationResult(httpContext, formResult.Result!);
          }

          var form = formResult.Form!;
          var file = GetUploadedFile(form);
          if (file is null)
          {
            return ApplicationEndpointResults.FromOperationResult(
              httpContext,
              InvalidDocumentUpload("file", ValidationMessageKeys.Required));
          }

          if (file.Length <= 0 || file.Length > DocumentCatalog.MaxFileSizeBytes)
          {
            return ApplicationEndpointResults.FromOperationResult(
              httpContext,
              InvalidDocumentUpload("sizeBytes", "validation.fileSize"));
          }

          var validContractId = TryReadOptionalGuid(form, "contractId", out var contractId, out var contractError);
          var validPropertyId = TryReadOptionalGuid(form, "propertyId", out var propertyId, out var propertyError);
          if (!validContractId || !validPropertyId)
          {
            return ApplicationEndpointResults.FromOperationResult(
              httpContext,
              InvalidDocumentUpload(contractError ?? propertyError ?? "id", ValidationMessageKeys.InvalidId));
          }

          using var content = file.OpenReadStream();
          var request = new ResidentPortalDocumentUploadRequestDto(
            ReadFormValue(form, "category"),
            ReadFormValue(form, "title"),
            ReadOptionalFormValue(form, "description"),
            contractId,
            propertyId,
            file.FileName,
            file.ContentType,
            file.Length,
            content,
            ReadOptionalFormValue(form, "versionNotes"));
          var result = await residentPortalService.UploadDocumentAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_UploadDocument")
      .WithSummary("Uploads a resident portal document when organization settings allow it.")
      .WithMetadata(
        new RequestSizeLimitAttribute(MaxMultipartRequestBodyBytes),
        new RequestFormLimitsAttribute { MultipartBodyLengthLimit = DocumentCatalog.MaxFileSizeBytes })
      .Accepts<IFormFile>("multipart/form-data")
      .Produces<ResidentPortalDocumentDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapGet(
        "/documents/{documentId:guid}/download",
        async (
          Guid documentId,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.DownloadDocumentAsync(documentId, cancellationToken)
            .ConfigureAwait(false);
          return ToFileResult(httpContext, result);
        })
      .WithName("ResidentPortal_DownloadDocument")
      .WithSummary("Downloads a resident-visible document.")
      .Produces(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    portal.MapGet(
        "/occurrences",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.ListOccurrencesAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_Occurrences")
      .WithSummary("Lists resident occurrences.")
      .Produces<IReadOnlyList<ResidentPortalOccurrenceDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapPost(
        "/occurrences",
        async (
          ResidentPortalOccurrenceCreateRequestDto request,
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.CreateOccurrenceAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_CreateOccurrence")
      .WithSummary("Creates a resident occurrence when organization settings allow it.")
      .Produces<ResidentPortalOccurrenceDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapGet(
        "/inspections",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.ListInspectionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_Inspections")
      .WithSummary("Lists inspections visible to the resident.")
      .Produces<IReadOnlyList<ResidentPortalInspectionDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    portal.MapGet(
        "/notifications",
        async (
          string? locale,
          IResidentPortalService residentPortalService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentPortalService.ListNotificationsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("ResidentPortal_Notifications")
      .WithSummary("Lists resident portal notifications.")
      .Produces<IReadOnlyList<ResidentPortalNotificationDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    return v1;
  }

  private static async Task<UploadFormResult> ReadUploadFormAsync(
    HttpContext httpContext,
    CancellationToken cancellationToken)
  {
    if (!httpContext.Request.HasFormContentType)
    {
      return UploadFormResult.Invalid(InvalidDocumentUpload("contentType", "validation.multipart"));
    }

    try
    {
      var form = await httpContext.Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
      return UploadFormResult.Success(form);
    }
    catch (BadHttpRequestException)
    {
      return UploadFormResult.Invalid(InvalidDocumentUpload("sizeBytes", "validation.fileSize"));
    }
    catch (InvalidDataException)
    {
      return UploadFormResult.Invalid(InvalidDocumentUpload("sizeBytes", "validation.fileSize"));
    }
  }

  private static IResult ToFileResult(
    HttpContext httpContext,
    ApplicationOperationResult<DocumentDownloadDto> result)
  {
    if (!result.Succeeded)
    {
      return ApplicationEndpointResults.FromOperationResult(httpContext, result);
    }

    var download = result.Value!;
    return Results.File(
      download.Content,
      download.ContentType,
      fileDownloadName: download.FileName,
      enableRangeProcessing: true);
  }

  private static bool TryReadOptionalGuid(
    IFormCollection form,
    string key,
    out Guid? value,
    out string? errorProperty)
  {
    var text = ReadOptionalFormValue(form, key);
    if (text is null)
    {
      value = null;
      errorProperty = null;
      return true;
    }

    if (Guid.TryParse(text, out var parsed))
    {
      value = parsed;
      errorProperty = null;
      return true;
    }

    value = null;
    errorProperty = key;
    return false;
  }

  private static string ReadFormValue(IFormCollection form, string key) =>
    form.TryGetValue(key, out var value) ? value.ToString() : string.Empty;

  private static IFormFile? GetUploadedFile(IFormCollection form) =>
    form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);

  private static string? ReadOptionalFormValue(IFormCollection form, string key)
  {
    var value = ReadFormValue(form, key);
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private static ApplicationOperationResult<ResidentPortalDocumentDto> InvalidDocumentUpload(
    string property,
    string messageKey) =>
    ApplicationOperationResult<ResidentPortalDocumentDto>.Invalid([new ValidationFailure(property, messageKey)]);

  private sealed record UploadFormResult(
    bool Succeeded,
    IFormCollection? Form,
    ApplicationOperationResult<ResidentPortalDocumentDto>? Result)
  {
    public static UploadFormResult Success(IFormCollection form) => new(true, form, null);

    public static UploadFormResult Invalid(ApplicationOperationResult<ResidentPortalDocumentDto> result) =>
      new(false, null, result);
  }
}
