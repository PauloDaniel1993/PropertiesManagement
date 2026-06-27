using Alsappan.Application.Common.Validation;

namespace Alsappan.Application.Tests;

public sealed class ValidationContractTests
{
  [Fact]
  public void ValidationResultGroupsFailuresByField()
  {
    var result = ValidationResult.Invalid(
    [
      new ValidationFailure("name", ValidationMessageKeys.Required),
      new ValidationFailure("name", ValidationMessageKeys.MaxLength),
      new ValidationFailure("email", ValidationMessageKeys.Email)
    ]);

    var errors = result.ToErrorDictionary();

    Assert.False(result.IsValid);
    Assert.Equal(
      [ValidationMessageKeys.Required, ValidationMessageKeys.MaxLength],
      errors["name"]);
    Assert.Equal([ValidationMessageKeys.Email], errors["email"]);
  }

  [Fact]
  public void LocalizedValidationMessagesDefaultToBrazilianPortuguese()
  {
    var messages = new LocalizedValidationMessages();

    Assert.Equal("Campo obrigatorio.", messages.Resolve(ValidationMessageKeys.Required, null));
    Assert.Equal("Required field.", messages.Resolve(ValidationMessageKeys.Required, "en-US"));
  }

  [Theory]
  [MemberData(nameof(ApplicationValidationKeys))]
  public void LocalizedValidationMessagesCoverApplicationValidationKeys(string messageKey)
  {
    var messages = new LocalizedValidationMessages();

    Assert.NotEqual(messageKey, messages.Resolve(messageKey, null));
    Assert.NotEqual(messageKey, messages.Resolve(messageKey, "en-US"));
  }

  [Fact]
  public void LocalizedValidationMessagesPreserveUnknownKeys()
  {
    var messages = new LocalizedValidationMessages();

    Assert.Equal("validation.futureRule", messages.Resolve("validation.futureRule", null));
  }

  public static IEnumerable<object[]> ApplicationValidationKeys()
  {
    yield return new object[] { "validation.activeContract" };
    yield return new object[] { "validation.adjustmentIndex" };
    yield return new object[] { "validation.adjustmentInterval" };
    yield return new object[] { "validation.assignedUser" };
    yield return new object[] { "validation.assignee" };
    yield return new object[] { "validation.billingPeriod" };
    yield return new object[] { "validation.branding" };
    yield return new object[] { "validation.branding.colorContrast" };
    yield return new object[] { "validation.branding.colorHex" };
    yield return new object[] { "validation.branding.logoAltRequired" };
    yield return new object[] { "validation.branding.logoDimension" };
    yield return new object[] { "validation.branding.logoMimeType" };
    yield return new object[] { "validation.branding.logoSize" };
    yield return new object[] { "validation.catalogType" };
    yield return new object[] { "validation.category" };
    yield return new object[] { "validation.channel" };
    yield return new object[] { "validation.concurrency" };
    yield return new object[] { "validation.contract" };
    yield return new object[] { "validation.contractStatus" };
    yield return new object[] { "validation.currency" };
    yield return new object[] { "validation.currencyCode" };
    yield return new object[] { "validation.dateRange" };
    yield return new object[] { "validation.document" };
    yield return new object[] { "validation.documentCategory" };
    yield return new object[] { "validation.documentLink" };
    yield return new object[] { "validation.documentLinkAccess" };
    yield return new object[] { "validation.dueDay" };
    yield return new object[] { "validation.duplicate" };
    yield return new object[] { "validation.email" };
    yield return new object[] { "validation.emailTaken" };
    yield return new object[] { "validation.failed" };
    yield return new object[] { "validation.fileSize" };
    yield return new object[] { "validation.fileType" };
    yield return new object[] { "validation.garage" };
    yield return new object[] { "validation.inspectionConditionRating" };
    yield return new object[] { "validation.inspectionDocumentKind" };
    yield return new object[] { "validation.inspectionType" };
    yield return new object[] { "validation.invalidDate" };
    yield return new object[] { "validation.invalidId" };
    yield return new object[] { "validation.lifecycleAction" };
    yield return new object[] { "validation.linkedEntity" };
    yield return new object[] { "validation.locale" };
    yield return new object[] { "validation.maxLength" };
    yield return new object[] { "validation.mfaPolicy" };
    yield return new object[] { "validation.minValue" };
    yield return new object[] { "validation.money" };
    yield return new object[] { "validation.moneyPositive" };
    yield return new object[] { "validation.occurrenceLink" };
    yield return new object[] { "validation.occurrencePriority" };
    yield return new object[] { "validation.occurrenceStatus" };
    yield return new object[] { "validation.occurrenceType" };
    yield return new object[] { "validation.page" };
    yield return new object[] { "validation.pageSize" };
    yield return new object[] { "validation.parkingGarage" };
    yield return new object[] { "validation.parkingSpaceDuplicate" };
    yield return new object[] { "validation.parkingSpaceIdentifier" };
    yield return new object[] { "validation.password" };
    yield return new object[] { "validation.paymentAlreadySettled" };
    yield return new object[] { "validation.paymentAmountBelowSettled" };
    yield return new object[] { "validation.paymentBalance" };
    yield return new object[] { "validation.paymentGrossAmount" };
    yield return new object[] { "validation.paymentLink" };
    yield return new object[] { "validation.paymentMethod" };
    yield return new object[] { "validation.paymentProvider" };
    yield return new object[] { "validation.petAuthorizationStatus" };
    yield return new object[] { "validation.petSpecies" };
    yield return new object[] { "validation.portalStatus" };
    yield return new object[] { "validation.primaryResident" };
    yield return new object[] { "validation.privacyFlags" };
    yield return new object[] { "validation.property" };
    yield return new object[] { "validation.propertyAvailability" };
    yield return new object[] { "validation.propertyContractMismatch" };
    yield return new object[] { "validation.providerEvent" };
    yield return new object[] { "validation.receiptDocument" };
    yield return new object[] { "validation.reconciliationStatus" };
    yield return new object[] { "validation.required" };
    yield return new object[] { "validation.resident" };
    yield return new object[] { "validation.residentContractMismatch" };
    yield return new object[] { "validation.residentPortalLinked" };
    yield return new object[] { "validation.residentPortalState" };
    yield return new object[] { "validation.residentRole" };
    yield return new object[] { "validation.residents" };
    yield return new object[] { "validation.role" };
    yield return new object[] { "validation.slug" };
    yield return new object[] { "validation.status" };
    yield return new object[] { "validation.type" };
    yield return new object[] { "validation.unsupported" };
    yield return new object[] { "validation.url" };
    yield return new object[] { "validation.utilityAccount" };
    yield return new object[] { "validation.utilityBalance" };
    yield return new object[] { "validation.utilityResponsibility" };
    yield return new object[] { "validation.utilityType" };
    yield return new object[] { "validation.vehicleAuthorizationStatus" };
    yield return new object[] { "validation.vehicleType" };
    yield return new object[] { "validation.vehicleYear" };
  }
}
