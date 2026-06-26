namespace Alsappan.Domain.Common.ValueObjects;

public sealed record Address
{
  public Address(
    string streetLine,
    string number,
    string? complement,
    string neighborhood,
    string city,
    string stateCode,
    string? postalCode,
    string countryCode = "BR")
  {
    StreetLine = Required(streetLine, nameof(streetLine));
    Number = Required(number, nameof(number));
    Complement = Optional(complement);
    Neighborhood = Required(neighborhood, nameof(neighborhood));
    City = Required(city, nameof(city));
    StateCode = NormalizeCode(stateCode, 2, nameof(stateCode));
    PostalCode = Optional(postalCode);
    CountryCode = NormalizeCode(countryCode, 2, nameof(countryCode));
  }

  public string StreetLine { get; }

  public string Number { get; }

  public string? Complement { get; }

  public string Neighborhood { get; }

  public string City { get; }

  public string StateCode { get; }

  public string? PostalCode { get; }

  public string CountryCode { get; }

  private static string Required(string value, string parameterName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    return value.Trim();
  }

  private static string? Optional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  private static string NormalizeCode(string value, int length, string parameterName)
  {
    var normalized = Required(value, parameterName).ToUpperInvariant();

    if (normalized.Length != length || normalized.Any(character => character is < 'A' or > 'Z'))
    {
      throw new ArgumentException($"Code must be {length} alphabetic characters.", parameterName);
    }

    return normalized;
  }
}
