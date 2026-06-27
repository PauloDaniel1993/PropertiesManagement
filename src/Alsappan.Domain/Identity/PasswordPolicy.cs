namespace Alsappan.Domain.Identity;

public sealed record PasswordPolicy
{
  public PasswordPolicy(
    int minimumLength = 12,
    bool requireUppercase = true,
    bool requireLowercase = true,
    bool requireDigit = true,
    bool requireNonAlphanumeric = true)
  {
    if (minimumLength < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(minimumLength), "Minimum password length must be greater than zero.");
    }

    MinimumLength = minimumLength;
    RequireUppercase = requireUppercase;
    RequireLowercase = requireLowercase;
    RequireDigit = requireDigit;
    RequireNonAlphanumeric = requireNonAlphanumeric;
  }

  public static PasswordPolicy Default { get; } = new();

  public int MinimumLength { get; }

  public bool RequireUppercase { get; }

  public bool RequireLowercase { get; }

  public bool RequireDigit { get; }

  public bool RequireNonAlphanumeric { get; }

  public PasswordPolicyResult Validate(string? password)
  {
    var failures = new List<string>();

    if (string.IsNullOrEmpty(password))
    {
      failures.Add(PasswordPolicyFailureCodes.Required);
      return PasswordPolicyResult.Invalid(failures);
    }

    if (password.Length < MinimumLength)
    {
      failures.Add(PasswordPolicyFailureCodes.TooShort);
    }

    if (RequireUppercase && !password.Any(char.IsUpper))
    {
      failures.Add(PasswordPolicyFailureCodes.RequiresUppercase);
    }

    if (RequireLowercase && !password.Any(char.IsLower))
    {
      failures.Add(PasswordPolicyFailureCodes.RequiresLowercase);
    }

    if (RequireDigit && !password.Any(char.IsDigit))
    {
      failures.Add(PasswordPolicyFailureCodes.RequiresDigit);
    }

    if (RequireNonAlphanumeric && !password.Any(character => !char.IsLetterOrDigit(character)))
    {
      failures.Add(PasswordPolicyFailureCodes.RequiresNonAlphanumeric);
    }

    return failures.Count == 0
      ? PasswordPolicyResult.Valid
      : PasswordPolicyResult.Invalid(failures);
  }
}

public sealed record PasswordPolicyResult
{
  private PasswordPolicyResult(IReadOnlyList<string> failureCodes)
  {
    FailureCodes = failureCodes;
  }

  public IReadOnlyList<string> FailureCodes { get; }

  public bool IsValid => FailureCodes.Count == 0;

  public static PasswordPolicyResult Valid { get; } = new([]);

  public static PasswordPolicyResult Invalid(IEnumerable<string> failureCodes)
  {
    ArgumentNullException.ThrowIfNull(failureCodes);

    var codes = failureCodes
      .Where(code => !string.IsNullOrWhiteSpace(code))
      .Select(code => code.Trim())
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();

    return codes.Length == 0 ? Valid : new PasswordPolicyResult(codes);
  }
}

public static class PasswordPolicyFailureCodes
{
  public const string Required = "password.required";
  public const string TooShort = "password.tooShort";
  public const string RequiresUppercase = "password.requiresUppercase";
  public const string RequiresLowercase = "password.requiresLowercase";
  public const string RequiresDigit = "password.requiresDigit";
  public const string RequiresNonAlphanumeric = "password.requiresNonAlphanumeric";
}
