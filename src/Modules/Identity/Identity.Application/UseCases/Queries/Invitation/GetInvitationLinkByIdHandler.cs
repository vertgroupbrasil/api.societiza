using Core.Application.Messaging.Queries;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Options;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Queries.Invitation;
using Identity.Shared.Response.Invitation;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.Queries.Invitation;

public class GetInvitationLinkByIdHandler(
    IIdentityProjection<ProjectionModel.InvitationLink> linkProjection,
    IIdentityProjection<ProjectionModel.User> userProjection,
    ICurrentUserService currentUser,
    IOptions<FrontendOptions> frontendOptions
) : IQueryHandler<GetInvitationLinkByIdQuery, InvitationLinkByIdResponse>
{
    public async Task<Result<InvitationLinkByIdResponse>> Handle(GetInvitationLinkByIdQuery request, CancellationToken cancellationToken)
    {
        if (!UserRole.TryFromName(currentUser.Role, out var callerRole))
            return Result.Failure<InvitationLinkByIdResponse>(
                new Error("InvitationLink.ValidationError", "Invalid caller role."));

        var link = await linkProjection.FindAsync(
            x => x.Id == request.InvitationLinkId && !x.IsDeleted,
            cancellationToken);

        if (link is null)
            return Result.Failure<InvitationLinkByIdResponse>(
                new NotFoundError(new("InvitationLink.NotFound", "Invitation link not found.")));

        var hasAccess = callerRole == UserRole.SystemAdmin
            || (callerRole == UserRole.AccountancyAdmin && link.AccountancyId == currentUser.AccountancyId);

        if (!hasAccess)
            return Result.Failure<InvitationLinkByIdResponse>(
                new ForbiddenError(new("InvitationLink.Forbidden", "You do not have permission to view this invitation link.")));

        var users = await userProjection.ListAsync(
            x => x.InvitationLinkId == link.Id && !x.IsDeleted,
            cancellationToken);

        var userResponses = users
            .OrderBy(x => x.CreatedAt)
            .Select(x => new InvitationLinkUserResponse(x.Id, x.FirstName, x.LastName, x.Email, x.CreatedAt))
            .ToList();

        var url = $"{frontendOptions.Value.BaseUrl}/auth/join?token={link.Token}";

        var response = new InvitationLinkByIdResponse(
            link.Id,
            link.TargetRole,
            link.Status,
            link.AccountancyId,
            link.CreatedByUserId,
            link.MaxUses,
            link.Uses,
            link.ExpiresAt,
            link.CreatedAt,
            link.UpdatedAt,
            url,
            userResponses);

        return Result.Success(response);
    }
}
