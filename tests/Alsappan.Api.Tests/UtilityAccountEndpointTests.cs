using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.UtilityAccounts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000, CA2234

public sealed class UtilityAccountEndpointTests
{
  private static readonly Guid UtilityAccountId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task UtilityAccountsListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/utility-accounts", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task UtilityAccountEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri(
      "/v1/utility-accounts?type=electricity&status=open&responsibility=contract",
      UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    foreach (var optionsPath in new[]
      {
        "/v1/utility-accounts/status-options",
        "/v1/utility-accounts/type-options",
        "/v1/utility-accounts/responsibility-options"
      })
    {
      using var optionsResponse = await client.GetAsync(new Uri(optionsPath, UriKind.Relative));
      optionsResponse.EnsureSuccessStatusCode();
    }

    using var detailResponse = await client.GetAsync(new Uri($"/v1/utility-accounts/{UtilityAccountId}", UriKind.Relative));
    detailResponse.EnsureSuccessStatusCode();

    using var createResponse = await client.PostAsJsonAsync("/v1/utility-accounts", CreateRequest());
    createResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync($"/v1/utility-accounts/{UtilityAccountId}", UpdateRequest());
    updateResponse.EnsureSuccessStatusCode();

    using var paidResponse = await client.PostAsJsonAsync(
      $"/v1/utility-accounts/{UtilityAccountId}/mark-paid",
      new UtilityMarkPaidRequestDto(
        new UtilityMoneyDto(300m, "BRL"),
        new DateOnly(2026, 6, 27),
        "Pix",
        "PIX-1",
        Guid.NewGuid(),
        "Pagamento recebido"));
    paidResponse.EnsureSuccessStatusCode();

    using var cancelResponse = await client.PostAsJsonAsync(
      $"/v1/utility-accounts/{UtilityAccountId}/cancel",
      new UtilityLifecycleRequestDto("Cancelamento"));
    cancelResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsync($"/v1/utility-accounts/{UtilityAccountId}/restore", null);
    restoreResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri(
      $"/v1/utility-accounts/{UtilityAccountId}",
      UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IUtilityAccountService>();
          services.AddSingleton<IUtilityAccountService, FakeUtilityAccountService>();
        });
      });

  private static UtilityAccountCreateRequestDto CreateRequest() =>
    new(
      "Energia junho",
      "Conta de energia",
      "electricity",
      "contract",
      null,
      Guid.NewGuid(),
      null,
      new DateOnly(2026, 6, 1),
      new DateOnly(2026, 6, 30),
      new DateOnly(2026, 6, 30),
      new UtilityMoneyDto(300m, "BRL"),
      Guid.NewGuid(),
      "Observacoes");

  private static UtilityAccountUpdateRequestDto UpdateRequest() =>
    new(
      "Energia junho atualizada",
      "Conta de energia",
      "electricity",
      "contract",
      null,
      Guid.NewGuid(),
      null,
      new DateOnly(2026, 6, 1),
      new DateOnly(2026, 6, 30),
      new DateOnly(2026, 6, 30),
      new UtilityMoneyDto(300m, "BRL"),
      Guid.NewGuid(),
      "Observacoes",
      "concurrency-token");

  private static UtilityAccountDetailDto CreateDetail()
  {
    var property = new UtilityEntitySummaryDto(Guid.NewGuid(), "Casa Calabria", "Rua Calabria, 82", "/imoveis?id=1");
    var contract = new UtilityEntitySummaryDto(Guid.NewGuid(), "Contrato Casa", "Casa Calabria", "/contratos?id=1");
    var resident = new UtilityEntitySummaryDto(Guid.NewGuid(), "Joao da Silva", null, "/moradores?id=1");

    return new UtilityAccountDetailDto(
      UtilityAccountId,
      "Energia junho",
      "Conta de energia",
      new StatusLabelDto("electricity", "Energia", StatusLabelTones.Warning),
      new StatusLabelDto("open", "Em aberto", StatusLabelTones.Warning),
      new StatusLabelDto("contract", "Contrato", StatusLabelTones.Success),
      property,
      contract,
      resident,
      new DateOnly(2026, 6, 1),
      new DateOnly(2026, 6, 30),
      new DateOnly(2026, 6, 30),
      new UtilityMoneyDto(300m, "BRL"),
      new UtilityMoneyDto(0m, "BRL"),
      new UtilityMoneyDto(300m, "BRL"),
      null,
      null,
      null,
      "Observacoes",
      [new UtilityDocumentDto(Guid.NewGuid(), "bill", "Conta", "Conta", "/documentos?utilityAccountId=1")],
      [],
      $"/timeline?entityType=utilityAccount&entityId={UtilityAccountId}",
      $"/auditoria?entityType=utilityAccount&entityId={UtilityAccountId}",
      DateTimeOffset.UtcNow,
      DateTimeOffset.UtcNow,
      null,
      "concurrency-token");
  }

  private static UtilityAccountListItemDto CreateListItem()
  {
    var detail = CreateDetail();
    return new UtilityAccountListItemDto(
      detail.Id,
      detail.Title,
      detail.Description,
      detail.Type,
      detail.Status,
      detail.Responsibility,
      detail.Property,
      detail.Contract,
      detail.Resident,
      detail.BillingPeriodStart,
      detail.BillingPeriodEnd,
      detail.DueDate,
      detail.Amount,
      detail.PaidAmount,
      detail.Balance,
      detail.PaidOn,
      detail.PaymentMethod,
      detail.BankReference,
      false,
      false,
      detail.CreatedAt,
      detail.UpdatedAt,
      detail.ConcurrencyToken);
  }

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

  private sealed class FakeUtilityAccountService : IUtilityAccountService
  {
    public Task<ApplicationOperationResult<PagedResultDto<UtilityAccountListItemDto>>> ListAsync(
      UtilityAccountListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<UtilityAccountListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<UtilityAccountListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<UtilityAccountDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<UtilityAccountDetailDto>> CreateAsync(
      UtilityAccountCreateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<UtilityAccountDetailDto>> UpdateAsync(
      Guid id,
      UtilityAccountUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<UtilityAccountDetailDto>> MarkPaidAsync(
      Guid id,
      UtilityMarkPaidRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<UtilityAccountDetailDto>> CancelAsync(
      Guid id,
      UtilityLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<UtilityAccountDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("open", "Em aberto")]);
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("electricity", "Energia")]);
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetResponsibilityOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("contract", "Contrato")]);
    }

    private static Task<ApplicationOperationResult<UtilityAccountDetailDto>> Detail(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<UtilityAccountDetailDto>.Success(CreateDetail()));
    }
  }
}

#pragma warning restore CA1812, CA2000, CA2234
