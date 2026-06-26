namespace Alsappan.Application.Common.Contracts;

public sealed record ListFilterDto
{
  public const int DefaultPage = 1;
  public const int DefaultPageSize = 20;
  public const int MaxPageSize = 100;

  public ListFilterDto(
    int page = DefaultPage,
    int pageSize = DefaultPageSize,
    string? search = null,
    string? sort = null,
    bool includeArchived = false,
    string? locale = null,
    DateOnly? from = null,
    DateOnly? to = null)
  {
    if (page < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
    }

    if (pageSize is < 1 or > MaxPageSize)
    {
      throw new ArgumentOutOfRangeException(nameof(pageSize), $"Page size must be between 1 and {MaxPageSize}.");
    }

    if (from.HasValue && to.HasValue && to.Value < from.Value)
    {
      throw new ArgumentException("End date filter cannot be before start date filter.", nameof(to));
    }

    Page = page;
    PageSize = pageSize;
    Search = Optional(search);
    Sort = Optional(sort);
    IncludeArchived = includeArchived;
    Locale = Optional(locale);
    From = from;
    To = to;
  }

  public int Page { get; }

  public int PageSize { get; }

  public string? Search { get; }

  public string? Sort { get; }

  public bool IncludeArchived { get; }

  public string? Locale { get; }

  public DateOnly? From { get; }

  public DateOnly? To { get; }

  public int Offset => (Page - 1) * PageSize;

  private static string? Optional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
