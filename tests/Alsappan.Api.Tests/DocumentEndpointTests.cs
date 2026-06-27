using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Documents;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000, CA2234

public sealed class DocumentEndpointTests
{
  private static readonly Guid DocumentId = new("77777777-7777-7777-7777-777777777777");
  private static readonly Guid LinkId = new("88888888-8888-8888-8888-888888888888");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task DocumentsListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/documents", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task DocumentEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri("/v1/documents?category=contract", UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    using var categoryResponse = await client.GetAsync(new Uri("/v1/documents/category-options", UriKind.Relative));
    categoryResponse.EnsureSuccessStatusCode();

    using var statusResponse = await client.GetAsync(new Uri("/v1/documents/status-options", UriKind.Relative));
    statusResponse.EnsureSuccessStatusCode();

    using var allowedTypesResponse = await client.GetAsync(new Uri("/v1/documents/allowed-file-types", UriKind.Relative));
    allowedTypesResponse.EnsureSuccessStatusCode();

    using var detailResponse = await client.GetAsync(new Uri($"/v1/documents/{DocumentId}", UriKind.Relative));
    detailResponse.EnsureSuccessStatusCode();

    using var uploadResponse = await client.PostAsync("/v1/documents", CreateUploadForm());
    uploadResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync($"/v1/documents/{DocumentId}", new DocumentUpdateRequestDto(
      "contract",
      "Contrato atualizado",
      null,
      [new DocumentLinkRequestDto("contract", LinkId, "Contrato 1")],
      "concurrency-token"));
    updateResponse.EnsureSuccessStatusCode();

    using var versionResponse = await client.PostAsync($"/v1/documents/{DocumentId}/versions", CreateVersionForm());
    versionResponse.EnsureSuccessStatusCode();

    using var downloadResponse = await client.GetAsync(new Uri($"/v1/documents/{DocumentId}/download", UriKind.Relative));
    downloadResponse.EnsureSuccessStatusCode();
    Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);
    Assert.Equal("pdf", await downloadResponse.Content.ReadAsStringAsync());

    using var versionDownloadResponse = await client.GetAsync(new Uri(
      $"/v1/documents/{DocumentId}/versions/1/download",
      UriKind.Relative));
    versionDownloadResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsync($"/v1/documents/{DocumentId}/restore", null);
    restoreResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri($"/v1/documents/{DocumentId}", UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  [Fact]
  public async Task DocumentUploadWithoutFileReturnsProblemDetails()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());
    using var form = new MultipartFormDataContent
    {
      { new StringContent("contract"), "category" },
      { new StringContent("Contrato sem arquivo"), "title" }
    };

    using var response = await client.PostAsync("/v1/documents", form);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("file", out _));
  }

  [Fact]
  public async Task DocumentUploadRejectsFilesOverConfiguredLimit()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());
    using var form = new MultipartFormDataContent
    {
      { new StringContent("contract"), "category" },
      { new StringContent("Contrato grande"), "title" }
    };
    var oversizedPayload = new byte[checked((int)DocumentCatalog.MaxFileSizeBytes + 1)];
    var file = new ByteArrayContent(oversizedPayload);
    file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
    form.Add(file, "file", "contrato.pdf");

    using var response = await client.PostAsync("/v1/documents", form);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("sizeBytes", out _));
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IDocumentService>();
          services.AddSingleton<IDocumentService, FakeDocumentService>();
        });
      });

  private static MultipartFormDataContent CreateUploadForm()
  {
    var form = new MultipartFormDataContent
    {
      { new StringContent("contract"), "category" },
      { new StringContent("Contrato assinado"), "title" },
      { new StringContent("Documento digitalizado"), "description" },
      { new StringContent("Versao inicial"), "versionNotes" },
      { new StringContent("[{\"entityType\":\"contract\",\"entityId\":\"" + LinkId + "\",\"label\":\"Contrato 1\"}]"), "linksJson" }
    };
    var file = new ByteArrayContent(Encoding.UTF8.GetBytes("pdf"));
    file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
    form.Add(file, "file", "contrato.pdf");
    return form;
  }

  private static MultipartFormDataContent CreateVersionForm()
  {
    var form = new MultipartFormDataContent
    {
      { new StringContent("Nova versao"), "notes" }
    };
    var file = new ByteArrayContent(Encoding.UTF8.GetBytes("pdf"));
    file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
    form.Add(file, "file", "contrato-v2.pdf");
    return form;
  }

  private static DocumentDetailDto CreateDetail(string status = "active") =>
    new(
      DocumentId,
      "Contrato assinado",
      "Documento digitalizado",
      "contrato.pdf",
      "application/pdf",
      3,
      "contract",
      "Contrato",
      new StatusLabelDto(status, status == "archived" ? "Arquivado" : "Ativo"),
      1,
      DateTimeOffset.UtcNow.AddMinutes(-10),
      DateTimeOffset.UtcNow.AddMinutes(-10),
      DateTimeOffset.UtcNow,
      status == "archived" ? DateTimeOffset.UtcNow : null,
      [new DocumentLinkDto("contract", LinkId, "Contrato 1", $"/contratos?id={LinkId}")],
      [new DocumentVersionDto(Guid.NewGuid(), 1, "contrato.pdf", "application/pdf", 3, DateTimeOffset.UtcNow.AddMinutes(-10), UserId, "Versao inicial")],
      $"/v1/documents/{DocumentId}/download",
      $"/timeline?entityType=document&entityId={DocumentId}",
      $"/auditoria?entityType=document&entityId={DocumentId}",
      "concurrency-token");

  private static DocumentListItemDto CreateListItem() =>
    new(
      DocumentId,
      "Contrato assinado",
      "Documento digitalizado",
      "contrato.pdf",
      "application/pdf",
      3,
      "contract",
      "Contrato",
      new StatusLabelDto("active", "Ativo", StatusLabelTones.Success),
      1,
      DateTimeOffset.UtcNow.AddMinutes(-10),
      DateTimeOffset.UtcNow,
      false,
      [new DocumentLinkDto("contract", LinkId, "Contrato 1", $"/contratos?id={LinkId}")],
      "concurrency-token");

  private static string CreateJwt()
  {
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
      "replace-this-dev-only-signing-key-with-at-least-32-characters"));
    var token = new JwtSecurityToken(
      issuer: "Alsappan",
      audience: "Alsappan.Web",
      claims:
      [
        new Claim(JwtRegisteredClaimNames.Sub, UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, "admin@alsappan.local")
      ],
      expires: DateTime.UtcNow.AddMinutes(15),
      signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private sealed class FakeDocumentService : IDocumentService
  {
    public Task<ApplicationOperationResult<PagedResultDto<DocumentListItemDto>>> ListAsync(
      DocumentListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<DocumentListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<DocumentListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<DocumentDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<DocumentDetailDto>.Success(CreateDetail()));
    }

    public Task<ApplicationOperationResult<DocumentDetailDto>> UploadAsync(
      DocumentUploadRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<DocumentDetailDto>.Success(CreateDetail()));
    }

    public Task<ApplicationOperationResult<DocumentDetailDto>> UpdateAsync(
      Guid id,
      DocumentUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<DocumentDetailDto>.Success(CreateDetail()));
    }

    public Task<ApplicationOperationResult<DocumentDetailDto>> UploadVersionAsync(
      Guid id,
      DocumentVersionUploadRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<DocumentDetailDto>.Success(CreateDetail()));
    }

    public Task<ApplicationOperationResult<DocumentDownloadDto>> DownloadAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<DocumentDownloadDto>.Success(new DocumentDownloadDto(
        "contrato.pdf",
        "application/pdf",
        3,
        new MemoryStream(Encoding.UTF8.GetBytes("pdf")))));
    }

    public Task<ApplicationOperationResult<DocumentDownloadDto>> DownloadVersionAsync(
      Guid id,
      int versionNumber,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return DownloadAsync(id, cancellationToken);
    }

    public Task<ApplicationOperationResult> ArchiveAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<DocumentDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<DocumentDetailDto>.Success(CreateDetail()));
    }

    public Task<IReadOnlyList<SelectOptionDto>> GetCategoryOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<SelectOptionDto>>([new SelectOptionDto("contract", "Contrato")]);
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("active", "Ativo")]);
    }

    public Task<IReadOnlyList<SelectOptionDto>> GetAllowedFileTypeOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<SelectOptionDto>>([new SelectOptionDto(".pdf", "Arquivo .pdf")]);
    }
  }
}
