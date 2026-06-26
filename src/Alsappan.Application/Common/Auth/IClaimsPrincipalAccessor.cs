using System.Security.Claims;

namespace Alsappan.Application.Common.Auth;

public interface IClaimsPrincipalAccessor
{
  ClaimsPrincipal? Principal { get; }
}
