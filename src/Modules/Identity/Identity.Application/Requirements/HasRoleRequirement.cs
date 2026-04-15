using Microsoft.AspNetCore.Authorization;

namespace Identity.Application.Requirements;

public sealed class HasRoleRequirement(params string[] roles) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Roles { get; } = roles;
}