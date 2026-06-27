using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Payments;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000, CA2234

public sealed class PaymentEndpointTests
{
  private static readonly Guid PaymentId = new("99999999-9999-9999-9999-999999999999");
  private static readonly Guid TransactionId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task PaymentsListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/payments", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task PaymentEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri("/v1/payments?status=pending", UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    foreach (var optionsPath in new[]
      {
        "/v1/payments/status-options",
        "/v1/payments/method-options",
        "/v1/payments/reconciliation-status-options",
        "/v1/payments/provider-options"
      })
    {
      using var optionsResponse = await client.GetAsync(new Uri(optionsPath, UriKind.Relative));
      optionsResponse.EnsureSuccessStatusCode();
    }

    using var detailResponse = await client.GetAsync(new Uri($"/v1/payments/{PaymentId}", UriKind.Relative));
    detailResponse.EnsureSuccessStatusCode();

    using var createResponse = await client.PostAsJsonAsync("/v1/payments", CreateRequest());
    createResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync($"/v1/payments/{PaymentId}", UpdateRequest());
    updateResponse.EnsureSuccessStatusCode();

    using var transactionResponse = await client.PostAsJsonAsync(
      $"/v1/payments/{PaymentId}/transactions",
      new PaymentTransactionRequestDto(
        new PaymentMoneyDto(100m, "BRL"),
        "pix",
        new DateOnly(2026, 6, 27),
        "PIX-1",
        null,
        null,
        null,
        null));
    transactionResponse.EnsureSuccessStatusCode();

    using var reversalResponse = await client.PostAsJsonAsync(
      $"/v1/payments/{PaymentId}/transactions/reverse",
      new PaymentTransactionReversalRequestDto(TransactionId, "Estorno"));
    reversalResponse.EnsureSuccessStatusCode();

    using var cancelResponse = await client.PostAsJsonAsync(
      $"/v1/payments/{PaymentId}/cancel",
      new PaymentLifecycleRequestDto("Cancelamento"));
    cancelResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsync($"/v1/payments/{PaymentId}/restore", null);
    restoreResponse.EnsureSuccessStatusCode();

    using var instructionResponse = await client.PostAsJsonAsync(
      $"/v1/payments/{PaymentId}/instructions",
      new PaymentInstructionRequestDto("mock-pix"));
    instructionResponse.EnsureSuccessStatusCode();

    using var providerEventResponse = await client.PostAsJsonAsync(
      "/v1/payments/provider-events",
      new PaymentProviderEventRequestDto(
        "mock-pix",
        "mock-pix-reference",
        "settled",
        new DateOnly(2026, 6, 27)));
    providerEventResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri($"/v1/payments/{PaymentId}", UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IPaymentService>();
          services.AddSingleton<IPaymentService, FakePaymentService>();
        });
      });

  private static PaymentCreateRequestDto CreateRequest() =>
    new(
      "Aluguel junho",
      "Mensalidade",
      Guid.NewGuid(),
      null,
      null,
      null,
      new DateOnly(2026, 6, 30),
      new PaymentMoneyDto(1000m, "BRL"),
      null,
      null,
      "pix",
      "pending",
      "Observacoes");

  private static PaymentUpdateRequestDto UpdateRequest() =>
    new(
      "Aluguel junho atualizado",
      "Mensalidade",
      Guid.NewGuid(),
      null,
      null,
      null,
      new DateOnly(2026, 6, 30),
      new PaymentMoneyDto(1000m, "BRL"),
      null,
      null,
      "pix",
      "pending",
      "Observacoes",
      "concurrency-token");

  private static PaymentDetailDto CreateDetail() =>
    new(
      PaymentId,
      "Aluguel junho",
      "Mensalidade",
      new PaymentEntitySummaryDto(Guid.NewGuid(), "Contrato Casa", "Casa Calabria", "/contratos?id=1"),
      new PaymentEntitySummaryDto(Guid.NewGuid(), "Casa Calabria", "Rua Calabria, 82", "/imoveis?id=1"),
      new PaymentEntitySummaryDto(Guid.NewGuid(), "Joao da Silva", null, "/moradores?id=1"),
      null,
      new DateOnly(2026, 6, 30),
      new StatusLabelDto("pending", "Pendente", StatusLabelTones.Warning),
      new PaymentMoneyDto(1000m, "BRL"),
      new PaymentMoneyDto(0m, "BRL"),
      new PaymentMoneyDto(0m, "BRL"),
      new PaymentMoneyDto(1000m, "BRL"),
      new PaymentMoneyDto(0m, "BRL"),
      new PaymentMoneyDto(1000m, "BRL"),
      "pix",
      "Pix",
      new StatusLabelDto("pending", "Pendente", StatusLabelTones.Warning),
      "mock-pix",
      "mock-pix-reference",
      "{}",
      "Observacoes",
      [new PaymentTransactionDto(TransactionId, new PaymentMoneyDto(100m, "BRL"), "pix", "Pix", new DateOnly(2026, 6, 27), "PIX-1", null, null, null, null, false, DateTimeOffset.UtcNow)],
      [],
      $"/timeline?entityType=payment&entityId={PaymentId}",
      $"/auditoria?entityType=payment&entityId={PaymentId}",
      DateTimeOffset.UtcNow,
      DateTimeOffset.UtcNow,
      null,
      "concurrency-token");

  private static PaymentListItemDto CreateListItem()
  {
    var detail = CreateDetail();
    return new PaymentListItemDto(
      detail.Id,
      detail.Title,
      detail.Description,
      detail.Contract,
      detail.Property,
      detail.Resident,
      detail.DueDate,
      detail.Status,
      detail.Amount,
      detail.DiscountAmount,
      detail.PenaltyAmount,
      detail.GrossAmount,
      detail.SettledAmount,
      detail.Balance,
      detail.PreferredMethod,
      detail.PreferredMethodLabel,
      false,
      false,
      detail.CreatedAt,
      detail.UpdatedAt,
      detail.ConcurrencyToken);
  }

  private static PaymentInstructionDto CreateInstruction() =>
    new(
      PaymentId,
      "mock-pix",
      "mock-pix-reference",
      "pix",
      "issued",
      new PaymentMoneyDto(1000m, "BRL"),
      new DateOnly(2026, 6, 30),
      DateTimeOffset.UtcNow.AddHours(1),
      "Joao da Silva",
      null,
      null,
      "pix://mock/999",
      "PIX-COPIA-E-COLA",
      null,
      null,
      new Dictionary<string, string>(StringComparer.Ordinal) { ["mock"] = "true" });

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

  private sealed class FakePaymentService : IPaymentService
  {
    public Task<ApplicationOperationResult<PagedResultDto<PaymentListItemDto>>> ListAsync(
      PaymentListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<PaymentListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<PaymentListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<PaymentDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PaymentDetailDto>> CreateAsync(
      PaymentCreateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PaymentDetailDto>> UpdateAsync(
      Guid id,
      PaymentUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PaymentDetailDto>> RecordTransactionAsync(
      Guid id,
      PaymentTransactionRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PaymentDetailDto>> ReverseTransactionAsync(
      Guid id,
      PaymentTransactionReversalRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PaymentDetailDto>> CancelAsync(
      Guid id,
      PaymentLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult> ArchiveAsync(
      Guid id,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<PaymentDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PaymentInstructionDto>> CreateInstructionAsync(
      Guid id,
      PaymentInstructionRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<PaymentInstructionDto>.Success(CreateInstruction()));
    }

    public Task<ApplicationOperationResult<PaymentDetailDto>> ApplyProviderEventAsync(
      PaymentProviderEventRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("pending", "Pendente")]);
    }

    public Task<IReadOnlyList<SelectOptionDto>> GetMethodOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<SelectOptionDto>>([new SelectOptionDto("pix", "Pix")]);
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetReconciliationStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("pending", "Pendente")]);
    }

    public Task<IReadOnlyList<SelectOptionDto>> GetProviderOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<SelectOptionDto>>([new SelectOptionDto("mock-pix", "Pix mock")]);
    }

    private static Task<ApplicationOperationResult<PaymentDetailDto>> Detail(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<PaymentDetailDto>.Success(CreateDetail()));
    }
  }
}
