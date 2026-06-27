using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Providers;
using Alsappan.Domain.Payments;

namespace Alsappan.Infrastructure.Payments;

public sealed class MockPixPaymentInstructionProvider : IPaymentInstructionProvider
{
  public string ProviderCode => PaymentCatalog.MockPixProvider;

  public PaymentInstructionKind Kind => PaymentInstructionKind.Pix;

  public Task<PaymentInstructionDto> CreateInstructionAsync(
    PaymentProviderInstructionRequest request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    cancellationToken.ThrowIfCancellationRequested();

    var reference = $"{ProviderCode}-{request.ChargeId:N}";
    var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
    var copyPaste = $"00020126580014br.gov.bcb.pix0136{request.ChargeId:D}520400005303986540{request.Amount.Amount:0.00}5802BR";
    var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["mock"] = "true",
      ["provider"] = ProviderCode,
      ["kind"] = "pix",
      ["expiresAt"] = expiresAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
    };

    return Task.FromResult(new PaymentInstructionDto(
      request.ChargeId,
      ProviderCode,
      reference,
      "pix",
      "issued",
      request.Amount,
      request.DueDate,
      expiresAt,
      request.PayerSummary,
      null,
      null,
      $"pix://mock/{request.ChargeId:N}",
      copyPaste,
      null,
      null,
      metadata));
  }
}
