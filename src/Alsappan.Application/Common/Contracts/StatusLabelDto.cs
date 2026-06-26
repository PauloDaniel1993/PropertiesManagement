namespace Alsappan.Application.Common.Contracts;

public static class StatusLabelTones
{
  public const string Neutral = "neutral";
  public const string Success = "success";
  public const string Warning = "warning";
  public const string Danger = "danger";
  public const string Info = "info";
}

public sealed record StatusLabelDto
{
  public StatusLabelDto(string code, string label, string tone = StatusLabelTones.Neutral)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(label);
    ArgumentException.ThrowIfNullOrWhiteSpace(tone);

    Code = NormalizeToken(code);
    Label = label.Trim();
    Tone = NormalizeToken(tone);
  }

  public string Code { get; }

  public string Label { get; }

  public string Tone { get; }

  private static string NormalizeToken(string value) =>
    string.Concat(value.Trim().Select(char.ToLowerInvariant));
}
