namespace Alsappan.Application.Common.Contracts;

public sealed record PagedResultDto<TItem>
{
  public PagedResultDto(IReadOnlyList<TItem> items, int page, int pageSize, int totalItems)
  {
    ArgumentNullException.ThrowIfNull(items);

    if (page < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
    }

    if (pageSize < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
    }

    if (totalItems < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(totalItems), "Total item count cannot be negative.");
    }

    Items = items;
    Page = page;
    PageSize = pageSize;
    TotalItems = totalItems;
  }

  public IReadOnlyList<TItem> Items { get; }

  public int Page { get; }

  public int PageSize { get; }

  public int TotalItems { get; }

  public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);

  public bool HasPreviousPage => Page > 1;

  public bool HasNextPage => Page < TotalPages;
}
