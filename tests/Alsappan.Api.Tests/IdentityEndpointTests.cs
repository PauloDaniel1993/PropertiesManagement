using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Identity;
using Alsappan.Application.Identity.Administrators;
using Alsappan.Application.Identity.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using AppValidationFailure = Alsappan.Application.Common.Validation.ValidationFailure;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000

public sealed class IdentityEndpointTests
{
  private static readonly Guid OrganizationId = new("11111111-1111-1111-1111-111111111111");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");
  private static readonly string[] ManagerRoleCodes = ["organization.manager"];

  [Fact]
  public async Task AdminLoginReturnsSessionPayload()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.PostAsJsonAsync(
      "/v1/auth/admin/login",
      new { email = "admin@alsappan.local", password = "StrongPass123!" });

    response.EnsureSuccessStatusCode();
    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var root = payload.RootElement;

    Assert.Equal("access-token", root.GetProperty("accessToken").GetString());
    Assert.Equal("admin", root.GetProperty("user").GetProperty("accountType").GetString());
    Assert.Equal(OrganizationId, root.GetProperty("user").GetProperty("activeOrganizationId").GetGuid());
  }

  [Fact]
  public async Task LoginValidationFailureReturnsProblemDetails()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.PostAsJsonAsync(
      "/v1/auth/admin/login",
      new { email = "", password = "" });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("Email", out _));
  }

  [Fact]
  public async Task AdministratorsListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/administrators", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task AdministratorLifecycleEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri("/v1/administrators", UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    using var createResponse = await client.PostAsJsonAsync(
      "/v1/administrators",
      new
      {
        email = "gestor@alsappan.local",
        displayName = "Gestor Alsappan",
        roleCodes = ManagerRoleCodes
      });
    createResponse.EnsureSuccessStatusCode();

    using var deactivateResponse = await client.PostAsJsonAsync(
      $"/v1/administrators/{UserId}/deactivate",
      new { });
    deactivateResponse.EnsureSuccessStatusCode();

    using var reactivateResponse = await client.PostAsJsonAsync(
      $"/v1/administrators/{UserId}/reactivate",
      new { });
    reactivateResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(
      new Uri($"/v1/administrators/{UserId}", UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IIdentityAuthService>();
          services.RemoveAll<IAdministratorService>();
          services.AddSingleton<IIdentityAuthService, FakeIdentityAuthService>();
          services.AddSingleton<IAdministratorService, FakeAdministratorService>();
        });
      });

  private static AuthSessionDto CreateSession() =>
    new(
      "access-token",
      "Bearer",
      DateTimeOffset.UtcNow.AddMinutes(15),
      "refresh-token",
      CreateCurrentUser());

  private static CurrentUserDto CreateCurrentUser() =>
    new(
      UserId,
      "admin@alsappan.local",
      "Administrador Alsappan",
      "admin",
      OrganizationId,
      [
        new OrganizationContextDto(
          OrganizationId,
          "alsappan",
          "Alsappan",
          "Alsappan",
          "pt-BR",
          "BRL",
          ["organization.admin"],
          ["administrators.read", "administrators.write", "administrators.manage"])
      ],
      ["administrators.read", "administrators.write", "administrators.manage"]);

  private static AdministratorDetailDto CreateAdministratorDetail(string status = "active") =>
    new(
      UserId,
      "admin@alsappan.local",
      "Administrador Alsappan",
      status,
      ["organization.admin"],
      ["administrators.read", "administrators.write", "administrators.manage"],
      DateTimeOffset.UtcNow.AddDays(-1),
      DateTimeOffset.UtcNow,
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

  private sealed class FakeIdentityAuthService : IIdentityAuthService
  {
    public Task<IdentityServiceResult<AuthSessionDto>> LoginAdminAsync(
      AdminLoginRequest request,
      IdentityRequestContext? requestContext = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      return string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)
        ? Task.FromResult(IdentityServiceResult<AuthSessionDto>.Invalid(
          [new AppValidationFailure(nameof(request.Email), ValidationMessageKeys.Required)]))
        : Task.FromResult(IdentityServiceResult<AuthSessionDto>.Success(CreateSession()));
    }

    public Task<IdentityServiceResult<AuthSessionDto>> LoginResidentAsync(
      ResidentLoginRequest request,
      IdentityRequestContext? requestContext = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityServiceResult<AuthSessionDto>.Unauthorized());
    }

    public Task<IdentityServiceResult<AuthSessionDto>> RefreshAsync(
      RefreshAuthSessionRequest request,
      IdentityRequestContext? requestContext = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityServiceResult<AuthSessionDto>.Success(CreateSession()));
    }

    public Task<IdentityServiceResult> LogoutAsync(
      LogoutAuthSessionRequest request,
      IdentityRequestContext? requestContext = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityServiceResult.Success());
    }

    public Task<IdentityServiceResult<CurrentUserDto>> GetCurrentUserAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityServiceResult<CurrentUserDto>.Success(CreateCurrentUser()));
    }

    public Task<IdentityServiceResult<AuthSessionDto>> SwitchOrganizationAsync(
      SwitchOrganizationRequest request,
      IdentityRequestContext? requestContext = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityServiceResult<AuthSessionDto>.Success(CreateSession()));
    }
  }

  private sealed class FakeAdministratorService : IAdministratorService
  {
    public Task<IdentityOperationResult<PagedResultDto<AdministratorListItemDto>>> ListAsync(
      AdministratorListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var item = new AdministratorListItemDto(
        UserId,
        "admin@alsappan.local",
        "Administrador Alsappan",
        "active",
        ["organization.admin"],
        DateTimeOffset.UtcNow.AddDays(-1),
        DateTimeOffset.UtcNow);
      var page = new PagedResultDto<AdministratorListItemDto>([item], request.Page, request.PageSize, 1);
      return Task.FromResult(IdentityOperationResult<PagedResultDto<AdministratorListItemDto>>.Success(page));
    }

    public Task<IdentityOperationResult<AdministratorDetailDto>> GetAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityOperationResult<AdministratorDetailDto>.Success(CreateAdministratorDetail()));
    }

    public Task<IdentityOperationResult<AdministratorDetailDto>> CreateAsync(
      AdministratorCreateRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityOperationResult<AdministratorDetailDto>.Success(CreateAdministratorDetail()));
    }

    public Task<IdentityOperationResult<AdministratorDetailDto>> UpdateAsync(
      Guid id,
      AdministratorUpdateRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityOperationResult<AdministratorDetailDto>.Success(CreateAdministratorDetail()));
    }

    public Task<IdentityOperationResult<AdministratorDetailDto>> DeactivateAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityOperationResult<AdministratorDetailDto>.Success(CreateAdministratorDetail("inactive")));
    }

    public Task<IdentityOperationResult<AdministratorDetailDto>> ReactivateAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityOperationResult<AdministratorDetailDto>.Success(CreateAdministratorDetail()));
    }

    public Task<IdentityOperationResult> ArchiveAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(IdentityOperationResult.Success());
    }

    public Task<IReadOnlyList<SelectOptionDto>> GetRoleOptionsAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      IReadOnlyList<SelectOptionDto> roles =
      [
        new("organization.admin", "Administrador"),
        new("organization.manager", "Gestor")
      ];
      return Task.FromResult(roles);
    }
  }
}
