namespace Alsappan.Application.Common.Contracts;

public sealed record SelectOptionDto
{
  public SelectOptionDto(string value, string label, bool isDisabled = false, string? description = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    ArgumentException.ThrowIfNullOrWhiteSpace(label);

    Value = value.Trim();
    Label = label.Trim();
    IsDisabled = isDisabled;
    Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
  }

  public string Value { get; }

  public string Label { get; }

  public bool IsDisabled { get; }

  public string? Description { get; }
}
