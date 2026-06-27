using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Providers;
using Alsappan.Domain.Payments;

namespace Alsappan.Infrastructure.Payments;

public sealed class MockPayPalPaymentInstructionProvider : IPaymentInstructionProvider
{
  public string ProviderCode => PaymentCatalog.MockPayPalProvider;

  public PaymentInstructionKind Kind => PaymentInstructionKind.PayPal;

  public Task<PaymentInstructionDto> CreateInstructionAsync(
    PaymentProviderInstructionRequest request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    cancellationToken.ThrowIfCancellationRequested();

    var token = request.ChargeId.ToString("N");
    var reference = $"{ProviderCode}-{token}";
    var intentId = $"PAYPAL-MOCK-{token[..12].ToUpperInvariant()}";
    var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["mock"] = "true",
      ["provider"] = ProviderCode,
      ["kind"] = "paypal",
      ["intentId"] = intentId
    };

    return Task.FromResult(new PaymentInstructionDto(
      request.ChargeId,
      ProviderCode,
      reference,
      "paypal",
      "created",
      request.Amount,
      request.DueDate,
      null,
      request.PayerSummary,
      null,
      null,
      null,
      null,
      intentId,
      $"https://paypal.mock/checkout/{token}",
      metadata));
  }
}
