using Alsappan.Domain.Payments;

namespace Alsappan.Application.Payments.Providers;

public sealed record PaymentProviderInstructionRequest(
  Guid ChargeId,
  string ProviderCode,
  PaymentInstructionKind Kind,
  PaymentMoneyDto Amount,
  DateOnly DueDate,
  string PayerSummary,
  string? Locale);

public interface IPaymentInstructionProvider
{
  string ProviderCode { get; }

  PaymentInstructionKind Kind { get; }

  Task<PaymentInstructionDto> CreateInstructionAsync(
    PaymentProviderInstructionRequest request,
    CancellationToken cancellationToken = default);
}
