using Identity.Application.Requirements;
using Identity.Domain.Enumerations;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Application.Policies;

public static class IdentityPolicies
{
    public const string CanInviteSystemAdmin         = nameof(CanInviteSystemAdmin);
    public const string CanInviteAccountancyAdmin    = nameof(CanInviteAccountancyAdmin);
    public const string CanInviteAccountancyEmployee = nameof(CanInviteAccountancyEmployee);
    public const string CanManageInvites             = nameof(CanManageInvites);
    public const string CanManageUsers               = nameof(CanManageUsers);
    public const string CanPromoteUser               = nameof(CanPromoteUser);
    public const string CanManageAccountancies       = nameof(CanManageAccountancies);
    public const string CanManageWorkflowTemplates   = nameof(CanManageWorkflowTemplates);

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(CanInviteSystemAdmin, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name)));

        options.AddPolicy(CanInviteAccountancyAdmin, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name,
                UserRole.AccountancyAdmin.Name)));

        options.AddPolicy(CanInviteAccountancyEmployee, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.AccountancyAdmin.Name)));

        options.AddPolicy(CanManageInvites, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name,
                UserRole.AccountancyAdmin.Name)));

        options.AddPolicy(CanManageUsers, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name,
                UserRole.AccountancyAdmin.Name)));

        options.AddPolicy(CanPromoteUser, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.AccountancyAdmin.Name)));

        options.AddPolicy(CanManageAccountancies, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name)));

        options.AddPolicy(CanManageWorkflowTemplates, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name)));
    }
}