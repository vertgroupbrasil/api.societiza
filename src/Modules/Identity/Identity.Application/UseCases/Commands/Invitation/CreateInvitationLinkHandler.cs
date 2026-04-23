using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Options;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands.Invitation;
using Identity.Shared.Response.Invitation;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.Commands.Invitation;

using InvitationLinkAggregate = Identity.Domain.Aggregates.InvitationLink;

public class CreateInvitationLinkHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.InvitationLink> linkProjection,
    IAccountancyDataService accountancyDataService,
    ICurrentUserService currentUser,
    IOptions<FrontendOptions> frontendOptions
) : ICommandHandler<CreateInvitationLinkCommand, CreateInvitationLinkResponse>
{
    public async Task<Result<CreateInvitationLinkResponse>> Handle(CreateInvitationLinkCommand request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;

        if (!UserRole.TryFromName(currentUser.Role, out var callerRole))
            return Result.Failure<CreateInvitationLinkResponse>(
                new Error("InvitationLink.ValidationError", "Invalid caller role."));

        var targetRole = UserRole.FromName(request.TargetRole);

        var allowed = (callerRole == UserRole.SystemAdmin
            && (targetRole == UserRole.SystemAdmin || targetRole == UserRole.AccountancyAdmin))
            || (callerRole == UserRole.AccountancyAdmin
            && (targetRole == UserRole.AccountancyAdmin || targetRole == UserRole.AccountancyEmployee));

        if (!allowed)
            return Result.Failure<CreateInvitationLinkResponse>(
                new ForbiddenError(new("InvitationLink.Forbidden", "You do not have permission to create this type of link.")));

        Guid? accountancyId;

        if (targetRole == UserRole.SystemAdmin)
        {
            if (request.AccountancyId is not null)
                return Result.Failure<CreateInvitationLinkResponse>(
                    new Error("InvitationLink.ValidationError", "AccountancyId must not be provided for SA→SA links."));

            accountancyId = null;
        }
        else if (callerRole == UserRole.SystemAdmin && targetRole == UserRole.AccountancyAdmin)
        {
            if (request.AccountancyId is null)
                return Result.Failure<CreateInvitationLinkResponse>(
                    new Error("InvitationLink.ValidationError", "AccountancyId is required for SA→AA links."));

            var exists = await accountancyDataService.ExistsAsync(request.AccountancyId.Value, cancellationToken);

            if (!exists)
                return Result.Failure<CreateInvitationLinkResponse>(
                    new NotFoundError(new("Accountancy.NotFound", "Accountancy not found.")));

            accountancyId = request.AccountancyId;
        }
        else
        {
            accountancyId = currentUser.AccountancyId;
        }

        int maxUses;

        if (targetRole == UserRole.SystemAdmin || targetRole == UserRole.AccountancyAdmin)
        {
            maxUses = 1;
        }
        else
        {
            maxUses = request.MaxUses!.Value;
        }

        var activeLink = await linkProjection.FindAsync(
            x => x.TargetRole == targetRole.Name
              && x.AccountancyId == accountancyId
              && x.Status == InvitationLinkStatus.Active.Name
              && !x.IsDeleted,
            cancellationToken);

        if (activeLink is not null)
            return Result.Failure<CreateInvitationLinkResponse>(
                new ConflictError(new("InvitationLink.AlreadyActiveInScope",
                    "An active link already exists for this scope — revoke the current one before creating another.")));

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var link = InvitationLinkAggregate.Create(targetRole, accountancyId, callerId, rawToken, tokenHash, maxUses);
        await applicationService.AppendEventsAsync(link, cancellationToken);

        var url = $"{frontendOptions.Value.BaseUrl}/auth/join?token={rawToken}";
        return Result.Success(new CreateInvitationLinkResponse(link.Id, rawToken, url, link.ExpiresAt));
    }
}