using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Enumerations;
using Identity.Shared.Commands.Invitation;

namespace Identity.Application.UseCases.Commands.Invitation;

using InvitationLinkAggregate = Identity.Domain.Aggregates.InvitationLink;

public sealed class RevokeInvitationLinkHandler(
    IIdentityApplicationService applicationService,
    ICurrentUserService currentUser
) : ICommandHandler<RevokeInvitationLinkCommand>
{
    public async Task<Result> Handle(
        RevokeInvitationLinkCommand request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;

        if (!UserRole.TryFromName(currentUser.Role, out var callerRole))
            return Result.Failure(new Error("InvitationLink.ValidationError", "Invalid caller role."));

        var linkResult = await applicationService.LoadAggregateAsync<InvitationLinkAggregate>(request.InvitationLinkId, cancellationToken);

        if (!linkResult.IsSuccess)
            return Result.Failure(
                new NotFoundError(new("InvitationLink.NotFound", "Invitation link not found.")));

        var link = linkResult.Value;

        if (link.Status != InvitationLinkStatus.Active)
            return Result.Failure(
                new ConflictError(new("InvitationLink.NotRevocable", "The link is no longer active.")));

        if (callerRole == UserRole.SystemAdmin)
        {
            if (link.TargetRole != UserRole.SystemAdmin)
                return Result.Failure(
                    new ForbiddenError(new("InvitationLink.Forbidden", "SystemAdmin can only revoke SA→SA links.")));
        }
        else if (callerRole == UserRole.AccountingAdmin)
        {
            var canRevoke = link.AccountancyId == currentUser.AccountancyId
                && (link.TargetRole == UserRole.AccountingAdmin || link.TargetRole == UserRole.AccountingEmployee);

            if (!canRevoke)
                return Result.Failure(
                    new ForbiddenError(new("InvitationLink.Forbidden", "You do not have permission to revoke this link.")));
        }
        else
        {
            return Result.Failure(
                new ForbiddenError(new("InvitationLink.Forbidden", "You do not have permission to revoke invitation links.")));
        }

        link.Revoke(callerId);
        await applicationService.AppendEventsAsync(link, cancellationToken);

        return Result.Success();
    }
}