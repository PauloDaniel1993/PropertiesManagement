using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Providers;
using Alsappan.Domain.Payments;

namespace Alsappan.Infrastructure.Payments;

public sealed class MockBoletoPaymentInstructionProvider : IPaymentInstructionProvider
{
  public string ProviderCode => PaymentCatalog.MockBoletoProvider;

  public PaymentInstructionKind Kind => PaymentInstructionKind.Boleto;

  public Task<PaymentInstructionDto> CreateInstructionAsync(
    PaymentProviderInstructionRequest request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    cancellationToken.ThrowIfCancellationRequested();

    var reference = BuildReference(request.ChargeId);
    var token = request.ChargeId.ToString("N");
    var linhaDigitavel = $"34191.79001 01043.{token[..5]} 91020.{token.Substring(5, 5)} 8 000000{request.Amount.Amount:000000}";
    var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["mock"] = "true",
      ["provider"] = ProviderCode,
      ["kind"] = "boleto",
      ["dueDate"] = request.DueDate.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
    };

    return Task.FromResult(new PaymentInstructionDto(
      request.ChargeId,
      ProviderCode,
      reference,
      "boleto",
      "issued",
      request.Amount,
      request.DueDate,
      null,
      request.PayerSummary,
      $"3419{token[..24]}",
      linhaDigitavel,
      null,
      null,
      null,
      null,
      metadata));
  }

  private static string BuildReference(Guid chargeId) => $"{PaymentCatalog.MockBoletoProvider}-{chargeId:N}";
}
