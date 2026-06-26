namespace Alsappan.Domain.Common.ValueObjects;

public sealed record Money
{
  public Money(decimal amount, string currency)
  {
    Amount = amount;
    Currency = NormalizeCurrency(currency);
  }

  public decimal Amount { get; }

  public string Currency { get; }

  public static Money Zero(string currency) => new(0m, currency);

  public Money Add(Money other)
  {
    ArgumentNullException.ThrowIfNull(other);
    EnsureSameCurrency(other);
    return new Money(Amount + other.Amount, Currency);
  }

  public Money Subtract(Money other)
  {
    ArgumentNullException.ThrowIfNull(other);
    EnsureSameCurrency(other);
    return new Money(Amount - other.Amount, Currency);
  }

  private static string NormalizeCurrency(string currency)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(currency);
    var normalized = currency.Trim().ToUpperInvariant();

    if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
    {
      throw new ArgumentException("Currency must be a three-letter ISO 4217 code.", nameof(currency));
    }

    return normalized;
  }

  private void EnsureSameCurrency(Money other)
  {
    if (!StringComparer.Ordinal.Equals(Currency, other.Currency))
    {
      throw new InvalidOperationException("Money values must use the same currency.");
    }
  }
}
