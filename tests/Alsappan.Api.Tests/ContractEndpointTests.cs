using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using AppValidationFailure = Alsappan.Application.Common.Validation.ValidationFailure;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000

public sealed class ContractEndpointTests
{
  private static readonly Guid ContractId = new("44444444-4444-4444-4444-444444444444");
  private static readonly Guid PropertyId = new("33333333-3333-3333-3333-333333333333");
  private static readonly Guid ResidentId = new("55555555-5555-5555-5555-555555555555");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task ContractsListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/contracts", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task ContractLifecycleEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri("/v1/contracts?status=active", UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    using var createResponse = await client.PostAsJsonAsync("/v1/contracts", CreateRequest());
    createResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync(
      $"/v1/contracts/{ContractId}",
      CreateUpdateRequest());
    updateResponse.EnsureSuccessStatusCode();

    using var activateResponse = await client.PostAsJsonAsync(
      $"/v1/contracts/{ContractId}/activate",
      new { });
    activateResponse.EnsureSuccessStatusCode();

    using var terminateResponse = await client.PostAsJsonAsync(
      $"/v1/contracts/{ContractId}/terminate",
      new { effectiveDate = "2027-06-30" });
    terminateResponse.EnsureSuccessStatusCode();

    using var cancelResponse = await client.PostAsJsonAsync(
      $"/v1/contracts/{ContractId}/cancel",
      new { });
    cancelResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsJsonAsync(
      $"/v1/contracts/{ContractId}/restore",
      new { });
    restoreResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri($"/v1/contracts/{ContractId}", UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  [Fact]
  public async Task ContractValidationFailureReturnsProblemDetails()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    var invalid = CreateRequest() with { PropertyId = Guid.Empty };
    using var response = await client.PostAsJsonAsync("/v1/contracts", invalid);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("PropertyId", out _));
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IContractService>();
          services.AddSingleton<IContractService, FakeContractService>();
        });
      });

  private static ContractCreateRequestDto CreateRequest() =>
    new(
      PropertyId,
      ResidentId,
      [ResidentId],
      new DateOnly(2026, 7, 1),
      new DateOnly(2027, 6, 30),
      new ContractMoneyDto(2500m, "BRL"),
      10,
      new ContractMoneyDto(2500m, "BRL"),
      "ipca",
      12,
      new DateOnly(2027, 7, 1),
      "Multa",
      "Desconto",
      true,
      "Observacoes");

  private static ContractUpdateRequestDto CreateUpdateRequest() =>
    new(
      ResidentId,
      [ResidentId],
      new DateOnly(2026, 7, 1),
      new DateOnly(2027, 6, 30),
      new ContractMoneyDto(2600m, "BRL"),
      10,
      new ContractMoneyDto(2500m, "BRL"),
      "ipca",
      12,
      new DateOnly(2027, 7, 1),
      "Multa",
      "Desconto",
      true,
      "Observacoes");

  private static ContractDetailDto CreateDetail(string status = "active") =>
    new(
      ContractId,
      new ContractPropertySummaryDto(PropertyId, "Casa Calabria", "Rua Calabria, 82 - Vila Fazzione, Sao Paulo/SP"),
      new ContractPartyDto(ResidentId, "Joao da Silva", true),
      [new ContractPartyDto(ResidentId, "Joao da Silva", true)],
      new StatusLabelDto(status, status),
      new DateOnly(2026, 7, 1),
      new DateOnly(2027, 6, 30),
      new ContractMoneyDto(2500m, "BRL"),
      10,
      new ContractMoneyDto(2500m, "BRL"),
      "ipca",
      "IPCA",
      12,
      new DateOnly(2027, 7, 1),
      "Multa",
      "Desconto",
      true,
      "Observacoes",
      [],
      [],
      DateTimeOffset.UtcNow.AddDays(-1),
      DateTimeOffset.UtcNow,
      null,
      "concurrency-token");

  private static ContractListItemDto CreateListItem() =>
    new(
      ContractId,
      new ContractPropertySummaryDto(PropertyId, "Casa Calabria", "Rua Calabria, 82 - Vila Fazzione, Sao Paulo/SP"),
      new ContractPartyDto(ResidentId, "Joao da Silva", true),
      [new ContractPartyDto(ResidentId, "Joao da Silva", true)],
      new StatusLabelDto("active", "Ativo", StatusLabelTones.Success),
      new DateOnly(2026, 7, 1),
      new DateOnly(2027, 6, 30),
      new ContractMoneyDto(2500m, "BRL"),
      10,
      "ipca",
      "IPCA",
      false,
      DateTimeOffset.UtcNow.AddDays(-1),
      DateTimeOffset.UtcNow,
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

  private sealed class FakeContractService : IContractService
  {
    public Task<ApplicationOperationResult<PagedResultDto<ContractListItemDto>>> ListAsync(
      ContractListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<ContractListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<ContractListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<ContractDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<ContractDetailDto>.Success(CreateDetail()));
    }

    public Task<ApplicationOperationResult<ContractDetailDto>> CreateAsync(
      ContractCreateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return request.PropertyId == Guid.Empty
        ? Task.FromResult(ApplicationOperationResult<ContractDetailDto>.Invalid(
          [new AppValidationFailure(nameof(request.PropertyId), ValidationMessageKeys.InvalidId)]))
        : Task.FromResult(ApplicationOperationResult<ContractDetailDto>.Success(CreateDetail()));
    }

    public Task<ApplicationOperationResult<ContractDetailDto>> UpdateAsync(
      Guid id,
      ContractUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<ContractDetailDto>.Success(CreateDetail()));
    }

    public Task<ApplicationOperationResult<ContractDetailDto>> ActivateAsync(
      Guid id,
      ContractLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<ContractDetailDto>.Success(CreateDetail("active")));
    }

    public Task<ApplicationOperationResult<ContractDetailDto>> TerminateAsync(
      Guid id,
      ContractLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<ContractDetailDto>.Success(CreateDetail("terminated")));
    }

    public Task<ApplicationOperationResult<ContractDetailDto>> CancelAsync(
      Guid id,
      ContractLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<ContractDetailDto>.Success(CreateDetail("cancelled")));
    }

    public Task<ApplicationOperationResult> ArchiveAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<ContractDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<ContractDetailDto>.Success(CreateDetail("draft")));
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ContractCatalog.GetStatusOptions(locale));
    }

    public Task<IReadOnlyList<SelectOptionDto>> GetAdjustmentIndexOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ContractCatalog.GetAdjustmentIndexOptions(locale));
    }
  }
}
