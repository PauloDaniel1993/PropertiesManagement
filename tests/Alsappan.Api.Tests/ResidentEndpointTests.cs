using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Residents;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using AppValidationFailure = Alsappan.Application.Common.Validation.ValidationFailure;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000

public sealed class ResidentEndpointTests
{
  private static readonly Guid ResidentId = new("44444444-4444-4444-4444-444444444444");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task ResidentsListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/residents", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task ResidentLifecycleEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri("/v1/residents?search=joao", UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    using var statusOptions = await client.GetAsync(new Uri("/v1/residents/status-options", UriKind.Relative));
    statusOptions.EnsureSuccessStatusCode();

    using var portalStatusOptions = await client.GetAsync(new Uri("/v1/residents/portal-status-options", UriKind.Relative));
    portalStatusOptions.EnsureSuccessStatusCode();

    using var warningsResponse = await client.GetAsync(
      new Uri("/v1/residents/duplicate-warnings?email=joao@example.com", UriKind.Relative));
    warningsResponse.EnsureSuccessStatusCode();

    using var createResponse = await client.PostAsJsonAsync("/v1/residents", CreateRequest("Maria Souza"));
    createResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync(
      $"/v1/residents/{ResidentId}",
      CreateUpdateRequest("Maria Atualizada"));
    updateResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsJsonAsync(
      $"/v1/residents/{ResidentId}/restore",
      new { });
    restoreResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri($"/v1/residents/{ResidentId}", UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  [Fact]
  public async Task ResidentValidationFailureReturnsProblemDetails()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var response = await client.PostAsJsonAsync("/v1/residents", CreateRequest(""));

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("FullName", out _));
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IResidentService>();
          services.AddSingleton<IResidentService, FakeResidentService>();
        });
      });

  private static ResidentCreateRequestDto CreateRequest(string fullName) =>
    new(
      fullName,
      null,
      "maria@example.com",
      "(11) 98888-7777",
      null,
      "CPF",
      "987.654.321-00",
      new DateOnly(1992, 7, 3),
      new ResidentEmergencyContactDto("Ana Souza", "Irma", "(11) 97777-6666"),
      "active",
      "not-invited",
      ["identification-data"],
      "Observacoes");

  private static ResidentUpdateRequestDto CreateUpdateRequest(string fullName) =>
    new(
      fullName,
      null,
      "maria@example.com",
      "(11) 98888-7777",
      null,
      "CPF",
      "987.654.321-00",
      new DateOnly(1992, 7, 3),
      new ResidentEmergencyContactDto("Ana Souza", "Irma", "(11) 97777-6666"),
      "active",
      "not-invited",
      ["identification-data"],
      "Observacoes");

  private static ResidentDetailDto CreateDetail(string name = "Joao da Silva", string status = "active") =>
    new(
      ResidentId,
      name,
      null,
      "joao@example.com",
      "(11) 99999-8888",
      null,
      "CPF",
      "123.456.789-00",
      new DateOnly(1985, 1, 20),
      new ResidentEmergencyContactDto("Maria", "Mae", "(11) 98888-7777"),
      new StatusLabelDto(status, status == "inactive" ? "Inativo" : "Ativo"),
      new StatusLabelDto("not-invited", "Nao convidado"),
      ["identification-data"],
      "Observacoes",
      null,
      "joao@example.com",
      false,
      [],
      DateTimeOffset.UtcNow.AddDays(-1),
      DateTimeOffset.UtcNow,
      null,
      "concurrency-token");

  private static ResidentListItemDto CreateListItem() =>
    new(
      ResidentId,
      "Joao da Silva",
      null,
      "joao@example.com",
      "(11) 99999-8888",
      null,
      "CPF",
      "123.456.789-00",
      new ResidentEmergencyContactDto("Maria", "Mae", "(11) 98888-7777"),
      new StatusLabelDto("active", "Ativo", StatusLabelTones.Success),
      new StatusLabelDto("not-invited", "Nao convidado", StatusLabelTones.Neutral),
      ["identification-data"],
      "joao@example.com",
      false,
      false,
      DateTimeOffset.UtcNow.AddDays(-1),
      DateTimeOffset.UtcNow);

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

  private sealed class FakeResidentService : IResidentService
  {
    public Task<ApplicationOperationResult<PagedResultDto<ResidentListItemDto>>> ListAsync(
      ResidentListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<ResidentListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<ResidentListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<ResidentDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<ResidentDetailDto>.Success(CreateDetail()));
    }

    public Task<ApplicationOperationResult<ResidentDetailDto>> CreateAsync(
      ResidentCreateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return string.IsNullOrWhiteSpace(request.FullName)
        ? Task.FromResult(ApplicationOperationResult<ResidentDetailDto>.Invalid(
          [new AppValidationFailure(nameof(request.FullName), ValidationMessageKeys.Required)]))
        : Task.FromResult(ApplicationOperationResult<ResidentDetailDto>.Success(CreateDetail(request.FullName)));
    }

    public Task<ApplicationOperationResult<ResidentDetailDto>> UpdateAsync(
      Guid id,
      ResidentUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<ResidentDetailDto>.Success(CreateDetail(request.FullName)));
    }

    public Task<ApplicationOperationResult> ArchiveAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<ResidentDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<ResidentDetailDto>.Success(CreateDetail(status: "inactive")));
    }

    public Task<ApplicationOperationResult<IReadOnlyList<ResidentDuplicateWarningDto>>> GetDuplicateWarningsAsync(
      ResidentDuplicateWarningRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      IReadOnlyList<ResidentDuplicateWarningDto> warnings =
      [
        new("email", request.Email ?? string.Empty, ResidentId, "Joao da Silva", "Possivel morador duplicado.")
      ];
      return Task.FromResult(ApplicationOperationResult<IReadOnlyList<ResidentDuplicateWarningDto>>.Success(warnings));
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ResidentCatalog.GetStatusOptions(locale));
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetPortalStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ResidentCatalog.GetPortalStatusOptions(locale));
    }

    public Task<IReadOnlyList<SelectOptionDto>> GetPrivacyFlagOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ResidentCatalog.GetPrivacyFlagOptions(locale));
    }
  }
}
