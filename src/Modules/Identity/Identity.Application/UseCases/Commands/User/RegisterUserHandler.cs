using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Response;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands.User;

namespace Identity.Application.UseCases.Commands.User;

using UserAggregate = Identity.Domain.Aggregates.User;
using InvitationLinkAggregate = Identity.Domain.Aggregates.InvitationLink;

public sealed class RegisterUserHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.InvitationLink> linkProjection,
    IIdentityProjection<ProjectionModel.User> userProjection,
    IPasswordService passwordService
) : ICommandHandler<RegisterUserCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(
        RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var linkSnapshot = await linkProjection.FindAsync(x => x.TokenHash == tokenHash && !x.IsDeleted, cancellationToken);

        if (linkSnapshot is null)
            return Result.Failure<IdentifierResponse>(
                new NotFoundError(new("InvitationLink.NotFound", "Invitation link not found.")));

        var existingUser = await userProjection.FindAsync(x => x.Email == request.Email && !x.IsDeleted, cancellationToken);

        if (existingUser is not null)
            return Result.Failure<IdentifierResponse>(
                new ConflictError(new("User.AlreadyExists", "An account with this email already exists.")));

        var linkResult = await applicationService.LoadAggregateAsync<InvitationLinkAggregate>(linkSnapshot.Id, cancellationToken);

        if (!linkResult.IsSuccess)
            return Result.Failure<IdentifierResponse>(
                new NotFoundError(new("InvitationLink.NotFound", "Invitation link not found.")));

        var link = linkResult.Value;

        if (!link.IsUsable())
        {
            var (code, message) = GetNotUsableError(link);
            return Result.Failure<IdentifierResponse>(new Error(code, message));
        }

        var userId = Guid.NewGuid();
        var passwordHash = passwordService.HashPassword(request.Password);

        link.Consume(userId);

        var user = UserAggregate.Create(
            link.Id,
            request.FirstName,
            request.LastName,
            request.Email,
            passwordHash,
            link.TargetRole,
            link.AccountancyId);

        await applicationService.AppendEventsAsync(user, cancellationToken);
        await applicationService.AppendEventsAsync(link, cancellationToken);

        return Result.Success(new IdentifierResponse(user.Id));
    }

    private static (string Code, string Message) GetNotUsableError(InvitationLinkAggregate link)
    {
        if (link.ExpiresAt <= DateTimeOffset.UtcNow)
            return ("InvitationLink.Expired", "This invitation link has expired.");

        if (link.Status == InvitationLinkStatus.Exhausted)
            return ("InvitationLink.Exhausted", "This invitation link has reached its maximum number of uses.");

        if (link.Status == InvitationLinkStatus.Revoked)
            return ("InvitationLink.Revoked", "This invitation link has been revoked.");

        return ("InvitationLink.NotUsable", "This invitation link is no longer valid.");
    }
}