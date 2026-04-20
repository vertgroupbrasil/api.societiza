using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Queries;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Queries.Invitation;
using Identity.Shared.Response.Invitation;

namespace Identity.Application.UseCases.Queries.Invitation;

public class GetInvitationLinkByTokenHandler(
    IIdentityProjection<ProjectionModel.InvitationLink> linkProjection
) : IQueryHandler<GetInvitationLinkByTokenQuery, InvitationLinkPublicResponse>
{
    public async Task<Result<InvitationLinkPublicResponse>> Handle(
        GetInvitationLinkByTokenQuery request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var link = await linkProjection.FindAsync(x => x.TokenHash == tokenHash && !x.IsDeleted, cancellationToken);

        if (link is null)
            return Result.Failure<InvitationLinkPublicResponse>(
                new NotFoundError(new("InvitationLink.NotFound", "Invitation link not found.")));

        var effectiveStatus = link.Status == InvitationLinkStatus.Active.Name && link.ExpiresAt <= DateTimeOffset.UtcNow
            ? InvitationLinkStatus.Expired.Name
            : link.Status;

        return Result.Success(new InvitationLinkPublicResponse(link.TargetRole, effectiveStatus, link.AccountancyId));
    }
}