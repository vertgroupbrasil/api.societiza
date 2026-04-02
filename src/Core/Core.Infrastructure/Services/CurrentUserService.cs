using System.Security.Claims;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Core.Infrastructure.Services;

internal sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal User => accessor.HttpContext?.User
        ?? throw new InvalidOperationException("No active HTTP context.");

    public Guid UserId =>
        Guid.Parse(User.FindFirstValue("sub")!);

    public string Email =>
        User.FindFirstValue("email")!;

    public string Role =>
        User.FindFirstValue("role")!;

    public Guid? AccountancyId
    {
        get
        {
            var value = User.FindFirstValue("accountancy_id");
            return value is not null ? Guid.Parse(value) : null;
        }
    }

    public Guid? ResolveAccountancyId(Guid? routeAccountancyId)
    {
        if (Role == IdentityRoles.SystemAdmin)
            return routeAccountancyId;

        if (routeAccountancyId is null)
            return AccountancyId;

        if (AccountancyId != routeAccountancyId)
            throw new ForbiddenException();

        return AccountancyId;
    }
}
