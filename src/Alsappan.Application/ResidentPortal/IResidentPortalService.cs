using Alsappan.Application.Common.Results;
using Alsappan.Application.Documents;
using Alsappan.Application.Payments;

namespace Alsappan.Application.ResidentPortal;

public interface IResidentPortalService
{
  Task<ApplicationOperationResult<ResidentPortalSummaryDto>> GetSummaryAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentPortalProfileDto>> GetProfileAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentPortalPropertyDto?>> GetLinkedPropertyAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalContractDto>>> ListContractsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalPaymentDto>>> ListPaymentsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalDocumentDto>>> ListDocumentsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<DocumentDownloadDto>> DownloadDocumentAsync(
    Guid documentId,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalOccurrenceDto>>> ListOccurrencesAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalInspectionDto>>> ListInspectionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalNotificationDto>>> ListNotificationsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentPortalOccurrenceDto>> CreateOccurrenceAsync(
    ResidentPortalOccurrenceCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentPortalDocumentDto>> UploadDocumentAsync(
    ResidentPortalDocumentUploadRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PaymentInstructionDto>> CreatePaymentInstructionAsync(
    Guid paymentId,
    PaymentInstructionRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);
}
