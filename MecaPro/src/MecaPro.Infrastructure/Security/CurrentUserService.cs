using System;
using Microsoft.AspNetCore.Http;
using MecaPro.Application.Common;

namespace MecaPro.Infrastructure.Security;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value 
                          ?? httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
