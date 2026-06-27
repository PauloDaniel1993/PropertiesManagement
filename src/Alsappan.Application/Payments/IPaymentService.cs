using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Payments;

public interface IPaymentService
{
  Task<ApplicationOperationResult<PagedResultDto<PaymentListItemDto>>> ListAsync(
    PaymentListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentDetailDto>> CreateAsync(
    PaymentCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentDetailDto>> UpdateAsync(
    Guid id,
    PaymentUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentDetailDto>> RecordTransactionAsync(
    Guid id,
    PaymentTransactionRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentDetailDto>> ReverseTransactionAsync(
    Guid id,
    PaymentTransactionReversalRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentDetailDto>> CancelAsync(
    Guid id,
    PaymentLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentInstructionDto>> CreateInstructionAsync(
    Guid id,
    PaymentInstructionRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentDetailDto>> ApplyProviderEventAsync(
    PaymentProviderEventRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetMethodOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetReconciliationStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetProviderOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
