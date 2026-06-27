using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Search;

public interface IGlobalSearchService
{
  Task<ApplicationOperationResult<GlobalSearchResponseDto>> SearchAsync(
    GlobalSearchRequestDto request,
    CancellationToken cancellationToken = default);

  Task<GlobalSearchResultContractDto> GetContractAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
