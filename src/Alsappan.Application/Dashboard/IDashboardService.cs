using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Dashboard;

public interface IDashboardService
{
  Task<ApplicationOperationResult<DashboardOverviewDto>> GetOverviewAsync(
    DashboardOverviewRequestDto request,
    CancellationToken cancellationToken = default);
}
