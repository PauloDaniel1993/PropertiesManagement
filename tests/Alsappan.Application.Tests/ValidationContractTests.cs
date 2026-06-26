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

    Assert.Equal("Campo obrigatório.", messages.Resolve(ValidationMessageKeys.Required, null));
    Assert.Equal("Required field.", messages.Resolve(ValidationMessageKeys.Required, "en-US"));
  }
}
