using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Domain.Common.Identifiers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000

public sealed class AuditEndpointTests
{
  private static readonly Guid EntryId = new("55555555-5555-5555-5555-555555555555");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");
  private static readonly OrganizationId OrganizationId = new(new Guid("11111111-1111-1111-1111-111111111111"));

  [Fact]
  public async Task AuditListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/audit", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task AuditEndpointsReturnImmutableEntries()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri("/v1/audit?search=property&category=Mutation", UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());
    Assert.Equal("Property created", listPayload.RootElement.GetProperty("items")[0].GetProperty("actionLabel").GetString());

    using var optionsResponse = await client.GetAsync(new Uri("/v1/audit/category-options", UriKind.Relative));
    optionsResponse.EnsureSuccessStatusCode();

    using var getResponse = await client.GetAsync(new Uri($"/v1/audit/{EntryId}", UriKind.Relative));
    getResponse.EnsureSuccessStatusCode();
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IAuditService>();
          services.RemoveAll<IPermissionService>();
          services.AddSingleton<IAuditService, FakeAuditService>();
          services.AddSingleton<IPermissionService, AllowAuditPermissionService>();
        });
      });

  private static AuditEntryDto CreateEntry() =>
    new(
      EntryId,
      "property.created",
      "Property created",
      new AuditCategoryLabelDto("Mutation", "Data", "info"),
      DateTimeOffset.UtcNow,
      "user",
      UserId,
      "Paulo",
      "property",
      "property-a",
      "Calabria casa1",
      new Dictionary<string, string> { ["name"] = "Calabria casa1" },
      new Dictionary<string, string> { ["module"] = "properties" },
      "trace-a");

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
        new Claim(JwtRegisteredClaimNames.Email, "admin@alsappan.local"),
        new Claim("alsappan:active_organization_id", Guid.NewGuid().ToString()),
        new Claim("alsappan:permission", "*")
      ],
      expires: DateTime.UtcNow.AddMinutes(15),
      signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private sealed class FakeAuditService : IAuditService
  {
    public Task<ApplicationOperationResult<PagedResultDto<AuditEntryDto>>> ListAsync(
      AuditListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<AuditEntryDto>([CreateEntry()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<AuditEntryDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<AuditEntryDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<AuditEntryDto>.Success(CreateEntry()));
    }

    public Task<IReadOnlyList<AuditCategoryLabelDto>> GetCategoryOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      IReadOnlyList<AuditCategoryLabelDto> options =
      [
        new("Mutation", "Data", "info"),
        new("Security", "Security", "danger"),
        new("System", "System")
      ];
      return Task.FromResult(options);
    }
  }

  private sealed class AllowAuditPermissionService : IPermissionService
  {
    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      string permissionCode,
      CancellationToken cancellationToken = default) =>
      AuthorizeAsync(new PermissionRequirement(permissionCode), cancellationToken);

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      PermissionRequirement requirement,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult(PermissionEvaluationResult.Granted(requirement, OrganizationId));
    }

    public ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      IReadOnlySet<string> permissions = new HashSet<string>(
        [PermissionCodes.Read(PermissionModules.Audit)],
        StringComparer.Ordinal);
      return ValueTask.FromResult(permissions);
    }
  }
}
