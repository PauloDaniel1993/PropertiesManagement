using System.Text.Json;
using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Documents;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Documents;

#pragma warning disable CA1812
internal sealed class DocumentEndpointModule : IApiEndpointModule
{
  public int Order => 400;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapDocumentEndpoints();
}

internal static class DocumentEndpoints
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  public static RouteGroupBuilder MapDocumentEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var documents = v1.MapGroup("/documents")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Documents");

    documents.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? category,
          string? linkedEntityType,
          Guid? linkedEntityId,
          DateOnly? uploadedFrom,
          DateOnly? uploadedTo,
          Guid? uploadedByUserId,
          bool? includeArchived,
          string? locale,
          IDocumentService documentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new DocumentListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            category,
            linkedEntityType,
            linkedEntityId,
            uploadedFrom,
            uploadedTo,
            uploadedByUserId,
            includeArchived ?? false,
            locale);
          var result = await documentService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Documents_List")
      .WithSummary("Lists documents in the active organization.")
      .Produces<PagedResultDto<DocumentListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    documents.MapGet(
        "/category-options",
        async (
          string? locale,
          IDocumentService documentService,
          CancellationToken cancellationToken) =>
        {
          var options = await documentService.GetCategoryOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Documents_GetCategoryOptions")
      .WithSummary("Lists document category options.")
      .Produces<IReadOnlyList<SelectOptionDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    documents.MapGet(
        "/status-options",
        async (
          string? locale,
          IDocumentService documentService,
          CancellationToken cancellationToken) =>
        {
          var options = await documentService.GetStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Documents_GetStatusOptions")
      .WithSummary("Lists document lifecycle status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    documents.MapGet(
        "/allowed-file-types",
        async (
          string? locale,
          IDocumentService documentService,
          CancellationToken cancellationToken) =>
        {
          var options = await documentService.GetAllowedFileTypeOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Documents_GetAllowedFileTypes")
      .WithSummary("Lists allowed document file types.")
      .Produces<IReadOnlyList<SelectOptionDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    documents.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IDocumentService documentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await documentService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Documents_Get")
      .WithSummary("Gets document details.")
      .Produces<DocumentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    documents.MapPost(
        "",
        async (
          string? locale,
          IDocumentService documentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var formResult = await ReadUploadFormAsync(httpContext, cancellationToken)
            .ConfigureAwait(false);
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
              InvalidDetail("file", ValidationMessageKeys.Required));
          }

          if (!TryReadLinks(form, out var links, out var invalidLinks))
          {
            return ApplicationEndpointResults.FromOperationResult(httpContext, invalidLinks!);
          }

          using var content = file.OpenReadStream();
          var request = new DocumentUploadRequestDto(
            ReadFormValue(form, "category"),
            ReadFormValue(form, "title"),
            ReadOptionalFormValue(form, "description"),
            links,
            file.FileName,
            file.ContentType,
            file.Length,
            content,
            ReadOptionalFormValue(form, "versionNotes"));
          var result = await documentService.UploadAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Documents_Upload")
      .WithSummary("Uploads a document.")
      .Accepts<IFormFile>("multipart/form-data")
      .Produces<DocumentDetailDto>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    documents.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          DocumentUpdateRequestDto request,
          string? locale,
          IDocumentService documentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await documentService.UpdateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Documents_Update")
      .WithSummary("Updates document metadata and links.")
      .Produces<DocumentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    documents.MapPost(
        "/{id:guid}/versions",
        async (
          Guid id,
          string? locale,
          IDocumentService documentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var formResult = await ReadUploadFormAsync(httpContext, cancellationToken)
            .ConfigureAwait(false);
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
              InvalidDetail("file", ValidationMessageKeys.Required));
          }

          using var content = file.OpenReadStream();
          var request = new DocumentVersionUploadRequestDto(
            file.FileName,
            file.ContentType,
            file.Length,
            content,
            ReadOptionalFormValue(form, "notes"));
          var result = await documentService.UploadVersionAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Documents_UploadVersion")
      .WithSummary("Uploads a new document version.")
      .Accepts<IFormFile>("multipart/form-data")
      .Produces<DocumentDetailDto>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    documents.MapGet(
        "/{id:guid}/download",
        async (
          Guid id,
          IDocumentService documentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await documentService.DownloadAsync(id, cancellationToken)
            .ConfigureAwait(false);
          return ToFileResult(httpContext, result);
        })
      .WithName("Documents_Download")
      .WithSummary("Downloads the current document file.")
      .Produces(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    documents.MapGet(
        "/{id:guid}/versions/{versionNumber:int}/download",
        async (
          Guid id,
          int versionNumber,
          IDocumentService documentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await documentService.DownloadVersionAsync(id, versionNumber, cancellationToken)
            .ConfigureAwait(false);
          return ToFileResult(httpContext, result);
        })
      .WithName("Documents_DownloadVersion")
      .WithSummary("Downloads a historical document version file.")
      .Produces(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    documents.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IDocumentService documentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await documentService.RestoreAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Documents_Restore")
      .WithSummary("Restores an archived document.")
      .Produces<DocumentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    documents.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IDocumentService documentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await documentService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Documents_Archive")
      .WithSummary("Archives a document.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }

  private static async Task<UploadFormResult> ReadUploadFormAsync(
    HttpContext httpContext,
    CancellationToken cancellationToken)
  {
    if (!httpContext.Request.HasFormContentType)
    {
      return UploadFormResult.Invalid(InvalidDetail("contentType", "validation.multipart"));
    }

    var form = await httpContext.Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
    return UploadFormResult.Success(form);
  }

  private static bool TryReadLinks(
    IFormCollection form,
    out IReadOnlyList<DocumentLinkRequestDto> links,
    out ApplicationOperationResult<DocumentDetailDto>? invalidResult)
  {
    var json = ReadOptionalFormValue(form, "linksJson") ?? ReadOptionalFormValue(form, "links");
    if (string.IsNullOrWhiteSpace(json))
    {
      links = [];
      invalidResult = null;
      return true;
    }

    try
    {
      links = JsonSerializer.Deserialize<DocumentLinkRequestDto[]>(json, JsonOptions) ?? [];
      invalidResult = null;
      return true;
    }
    catch (JsonException)
    {
      links = [];
      invalidResult = InvalidDetail("links", "validation.documentLinks");
      return false;
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

  private static string ReadFormValue(IFormCollection form, string key) =>
    form.TryGetValue(key, out var value) ? value.ToString() : string.Empty;

  private static IFormFile? GetUploadedFile(IFormCollection form) =>
    form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);

  private static string? ReadOptionalFormValue(IFormCollection form, string key)
  {
    var value = ReadFormValue(form, key);
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private static ApplicationOperationResult<DocumentDetailDto> InvalidDetail(string property, string messageKey) =>
    ApplicationOperationResult<DocumentDetailDto>.Invalid([new ValidationFailure(property, messageKey)]);

  private sealed record UploadFormResult(
    bool Succeeded,
    IFormCollection? Form,
    ApplicationOperationResult<DocumentDetailDto>? Result)
  {
    public static UploadFormResult Success(IFormCollection form) => new(true, form, null);

    public static UploadFormResult Invalid(ApplicationOperationResult<DocumentDetailDto> result) =>
      new(false, null, result);
  }
}
