namespace Alsappan.Application.Common.Validation;

public sealed record ValidationFailure
{
  public ValidationFailure(string field, string messageKey)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(field);
    ArgumentException.ThrowIfNullOrWhiteSpace(messageKey);

    Field = field.Trim();
    MessageKey = messageKey.Trim();
  }

  public string Field { get; }

  public string MessageKey { get; }
}
