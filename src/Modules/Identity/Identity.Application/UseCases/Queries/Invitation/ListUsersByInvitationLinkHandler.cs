using Core.Application.Messaging.Queries;
using Core.Application.Services.Interfaces;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Queries.Invitation;
using Identity.Shared.Response.Invitation;

namespace Identity.Application.UseCases.Queries.Invitation;

public class ListUsersByInvitationLinkHandler(
    IIdentityProjection<ProjectionModel.InvitationLink> linkProjection,
    IIdentityProjection<ProjectionModel.User> userProjection,
    ICurrentUserService currentUser
) : IQueryHandler<ListUsersByInvitationLinkQuery, List<InvitationLinkUserResponse>>
{
    public async Task<Result<List<InvitationLinkUserResponse>>> Handle(
        ListUsersByInvitationLinkQuery request, CancellationToken cancellationToken)
    {
        if (!UserRole.TryFromName(currentUser.Role, out var callerRole))
            return Result.Failure<List<InvitationLinkUserResponse>>(
                new Error("InvitationLink.ValidationError", "Invalid caller role."));

        var link = await linkProjection.FindAsync(
            x => x.Id == request.InvitationLinkId && !x.IsDeleted,
            cancellationToken);

        if (link is null)
            return Result.Failure<List<InvitationLinkUserResponse>>(
                new NotFoundError(new("InvitationLink.NotFound", "Invitation link not found.")));

        if (callerRole == UserRole.AccountancyAdmin)
        {
            if (link.AccountancyId != currentUser.AccountancyId)
                return Result.Failure<List<InvitationLinkUserResponse>>(
                    new ForbiddenError(new("InvitationLink.Forbidden", "You do not have permission to view this link.")));
        }
        else if (callerRole != UserRole.SystemAdmin)
        {
            return Result.Failure<List<InvitationLinkUserResponse>>(
                new ForbiddenError(new("InvitationLink.Forbidden", "You do not have permission to view invitation link users.")));
        }

        var users = await userProjection.ListAsync(
            x => x.InvitationLinkId == request.InvitationLinkId && !x.IsDeleted,
            cancellationToken);

        var response = users
            .OrderBy(u => u.CreatedAt)
            .Select(u => new InvitationLinkUserResponse(u.Id, u.FirstName, u.LastName, u.Email, u.CreatedAt))
            .ToList();

        return Result.Success(response);
    }
}