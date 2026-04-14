using Identity.Application.Requirements;
using Identity.Domain.Enumerations;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Application.Policies;

public static class IdentityPolicies
{
    public const string CanInviteSystemAdmin        = nameof(CanInviteSystemAdmin);
    public const string CanInviteAccountingAdmin    = nameof(CanInviteAccountingAdmin);
    public const string CanInviteAccountingEmployee = nameof(CanInviteAccountingEmployee);
    public const string CanManageInvites            = nameof(CanManageInvites);
    public const string CanManageUsers              = nameof(CanManageUsers);
    public const string CanPromoteUser              = nameof(CanPromoteUser);
    public const string CanManageAccountancies      = nameof(CanManageAccountancies);
    public const string CanManageWorkflowTemplates  = nameof(CanManageWorkflowTemplates);

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(CanInviteSystemAdmin, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name)));

        options.AddPolicy(CanInviteAccountingAdmin, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name,
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanInviteAccountingEmployee, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanManageInvites, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name,
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanManageUsers, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name,
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanPromoteUser, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.AccountingAdmin.Name)));

        options.AddPolicy(CanManageAccountancies, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name)));

        options.AddPolicy(CanManageWorkflowTemplates, policy =>
            policy.AddRequirements(new HasRoleRequirement(
                UserRole.SystemAdmin.Name)));
    }
}