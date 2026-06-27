using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Properties;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using AppValidationFailure = Alsappan.Application.Common.Validation.ValidationFailure;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000

public sealed class PropertyEndpointTests
{
  private static readonly Guid PropertyId = new("33333333-3333-3333-3333-333333333333");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task PropertiesListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/properties", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task PropertyLifecycleEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri("/v1/properties?search=calabria", UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    using var createResponse = await client.PostAsJsonAsync("/v1/properties", CreateRequest("Imovel teste"));
    createResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync(
      $"/v1/properties/{PropertyId}",
      CreateRequest("Imovel atualizado"));
    updateResponse.EnsureSuccessStatusCode();

    using var statusResponse = await client.PostAsJsonAsync(
      $"/v1/properties/{PropertyId}/status",
      new { status = "maintenance" });
    statusResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsJsonAsync(
      $"/v1/properties/{PropertyId}/restore",
      new { });
    restoreResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri($"/v1/properties/{PropertyId}", UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  [Fact]
  public async Task PropertyValidationFailureReturnsProblemDetails()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var response = await client.PostAsJsonAsync("/v1/properties", CreateRequest(""));

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("Name", out _));
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IPropertyService>();
          services.AddSingleton<IPropertyService, FakePropertyService>();
        });
      });

  private static PropertyCreateRequestDto CreateRequest(string name) =>
    new(
      name,
      "house",
      "Para testes",
      new PropertyAddressDto("Rua Calabria", "82", "Casa 1", "Vila Fazzione", "Sao Paulo", "SP", "00000-000"),
      "available",
      new PropertyMoneyDto(700m, "BRL"),
      1,
      "A1",
      "Observacoes");

  private static PropertyDetailDto CreateDetail(string name = "Calabria casa1", string status = "available") =>
    new(
      PropertyId,
      name,
      "Sem descricao",
      "house",
      "Casa",
      new PropertyAddressDto("Rua Calabria", "82", "Casa 1", "Vila Fazzione", "Sao Paulo", "SP", "00000-000"),
      "Rua Calabria, 82 - Vila Fazzione, Sao Paulo/SP",
      new StatusLabelDto(status, status == "maintenance" ? "Manutencao" : "Disponivel"),
      new PropertyMoneyDto(700m, "BRL"),
      1,
      "A1",
      "1 vaga(s)",
      "Observacoes",
      [],
      DateTimeOffset.UtcNow.AddDays(-1),
      DateTimeOffset.UtcNow,
      null,
      "concurrency-token");

  private static PropertyListItemDto CreateListItem() =>
    new(
      PropertyId,
      "Calabria casa1",
      "Sem descricao",
      "house",
      "Casa",
      new PropertyAddressDto("Rua Calabria", "82", "Casa 1", "Vila Fazzione", "Sao Paulo", "SP", "00000-000"),
      "Rua Calabria, 82 - Vila Fazzione, Sao Paulo/SP",
      new StatusLabelDto("available", "Disponivel", StatusLabelTones.Success),
      new PropertyMoneyDto(700m, "BRL"),
      1,
      "A1",
      "1 vaga(s)",
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

  private sealed class FakePropertyService : IPropertyService
  {
    public Task<ApplicationOperationResult<PagedResultDto<PropertyListItemDto>>> ListAsync(
      PropertyListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<PropertyListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<PropertyListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<PropertyDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<PropertyDetailDto>.Success(CreateDetail()));
    }

    public Task<ApplicationOperationResult<PropertyDetailDto>> CreateAsync(
      PropertyCreateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return string.IsNullOrWhiteSpace(request.Name)
        ? Task.FromResult(ApplicationOperationResult<PropertyDetailDto>.Invalid(
          [new AppValidationFailure(nameof(request.Name), ValidationMessageKeys.Required)]))
        : Task.FromResult(ApplicationOperationResult<PropertyDetailDto>.Success(CreateDetail(request.Name)));
    }

    public Task<ApplicationOperationResult<PropertyDetailDto>> UpdateAsync(
      Guid id,
      PropertyUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<PropertyDetailDto>.Success(CreateDetail(request.Name)));
    }

    public Task<ApplicationOperationResult<PropertyDetailDto>> ChangeStatusAsync(
      Guid id,
      PropertyStatusChangeRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<PropertyDetailDto>.Success(CreateDetail(status: request.Status)));
    }

    public Task<ApplicationOperationResult> ArchiveAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<PropertyDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<PropertyDetailDto>.Success(CreateDetail(status: "inactive")));
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
  }
}
