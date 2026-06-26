using System.Security.Claims;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Tenancy;

namespace Alsappan.Api.Configuration;

internal sealed class HttpContextClaimsPrincipalAccessor : IClaimsPrincipalAccessor
{
  private readonly IHttpContextAccessor httpContextAccessor;

  public HttpContextClaimsPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
  {
    this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
  }

  public ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
}

internal sealed class HttpHeaderActiveOrganizationSelectionProvider : IActiveOrganizationSelectionProvider
{
  private readonly IHttpContextAccessor httpContextAccessor;

  public HttpHeaderActiveOrganizationSelectionProvider(IHttpContextAccessor httpContextAccessor)
  {
    this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
  }

  public string? GetRequestedOrganizationId()
  {
    var headers = httpContextAccessor.HttpContext?.Request.Headers;
    if (headers is null ||
      !headers.TryGetValue(TenancyHeaderNames.ActiveOrganizationId, out var requestedOrganizationId))
    {
      return null;
    }

    return requestedOrganizationId.FirstOrDefault();
  }
}
