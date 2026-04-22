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

public class ListInvitationLinksHandler(
    IIdentityProjection<ProjectionModel.InvitationLink> linkProjection,
    ICurrentUserService currentUser,
    IOptions<FrontendOptions> frontendOptions
) : IQueryHandler<ListInvitationLinksQuery, List<InvitationLinkDetailResponse>>
{
    public async Task<Result<List<InvitationLinkDetailResponse>>> Handle(ListInvitationLinksQuery request, CancellationToken cancellationToken)
    {
        if (!UserRole.TryFromName(currentUser.Role, out var callerRole))
            return Result.Failure<List<InvitationLinkDetailResponse>>(
                new Error("InvitationLink.ValidationError", "Invalid caller role."));

        var now = DateTimeOffset.UtcNow;
        List<ProjectionModel.InvitationLink> links;

        if (callerRole == UserRole.AccountancyAdmin)
        {
            links = await linkProjection.ListAsync(x => x.AccountancyId == currentUser.AccountancyId && !x.IsDeleted, cancellationToken);
        }
        else if (callerRole == UserRole.SystemAdmin)
        {
            if (request.AccountancyId is null)
            {
                links = await linkProjection.ListAsync(x => x.TargetRole == UserRole.SystemAdmin.Name && !x.IsDeleted, cancellationToken);
            } 
            else
            {
                links = await linkProjection.ListAsync(x => x.AccountancyId == request.AccountancyId && !x.IsDeleted, cancellationToken);
            }
        }
        else
        {
            return Result.Failure<List<InvitationLinkDetailResponse>>(
                new ForbiddenError(new("InvitationLink.Forbidden", "You do not have permission to list invitation links.")));
        }

        if (request.OnlyActive)
            links = links.Where(x => x.Status == InvitationLinkStatus.Active.Name && x.ExpiresAt > now).ToList();

        var baseUrl = frontendOptions.Value.BaseUrl;

        var response = links
            .OrderByDescending(x => x.Status == InvitationLinkStatus.Active.Name && x.ExpiresAt > now)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new InvitationLinkDetailResponse(
                x.Id,
                x.TargetRole,
                x.Status,
                x.AccountancyId,
                x.CreatedByUserId,
                x.MaxUses,
                x.Uses,
                x.CreatedAt,
                x.ExpiresAt,
                x.UpdatedAt,
                Url: $"{baseUrl}/auth/join?token={x.Token}"))
            .ToList();

        return Result.Success(response);
    }
}