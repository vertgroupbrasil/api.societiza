using Identity.Application.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Application.Authorization;

public sealed class HasRoleHandler : AuthorizationHandler<HasRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasRoleRequirement requirement)
    {
        var roleClaim = context.User.FindFirst("role")?.Value;

        if (roleClaim is not null && requirement.Roles.Contains(roleClaim))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}