namespace Alsappan.Application.Common.Validation;

public sealed record ValidationResult
{
  private ValidationResult(IReadOnlyList<ValidationFailure> failures)
  {
    Failures = failures;
  }

  public IReadOnlyList<ValidationFailure> Failures { get; }

  public bool IsValid => Failures.Count == 0;

  public static ValidationResult Valid { get; } = new([]);

  public static ValidationResult Invalid(IEnumerable<ValidationFailure> failures)
  {
    ArgumentNullException.ThrowIfNull(failures);

    var failureList = failures.ToArray();

    if (failureList.Length == 0)
    {
      return Valid;
    }

    return new ValidationResult(failureList);
  }

  public IDictionary<string, string[]> ToErrorDictionary(Func<string, string>? localize = null)
  {
    localize ??= static messageKey => messageKey;

    return Failures
      .GroupBy(failure => failure.Field, StringComparer.Ordinal)
      .ToDictionary(
        group => group.Key,
        group => group.Select(failure => localize(failure.MessageKey)).ToArray(),
        StringComparer.Ordinal);
  }
}
