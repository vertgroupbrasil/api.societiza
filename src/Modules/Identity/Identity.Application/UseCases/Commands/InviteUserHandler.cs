using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Options;
using Core.Shared.Errors;
using Core.Shared.Response;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands;
using Identity.Shared.Emails;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.Commands;

public class InviteUserHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.User> userProjection,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService,
    IJobSchedulerService jobScheduler,
    IOptions<FrontendOptions> frontendOptions
) : ICommandHandler<InviteUserCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await userProjection.FindAsync(x => x.Email == request.Email, cancellationToken);

        if (existingUser is not null)
            return Result.Failure<IdentifierResponse>(new ConflictError(new(
                "EmailAlreadyExists", "A user with this email already exists")));

        var role = Enum.Parse<UserRole>(request.Role);
        var passwordHash = passwordService.HashPassword(passwordService.GenerateRandomPassword());

        var user = User.Invite(
            name: request.Name,
            email: request.Email,
            passwordHash: passwordHash,
            role: role,
            accountancyId: request.AccountancyId
        );

        await applicationService.AppendEventsAsync(user, cancellationToken);

        var token = jwtTokenService.GeneratePasswordResetToken(user.Id, request.Email);
        var inviteLink = $"{frontendOptions.Value.BaseUrl}/auth/set-password?token={token}";

        jobScheduler.Enqueue(new SendInviteEmailCommand(
            ToEmail: request.Email,
            ToName: request.Name,
            InviteLink: inviteLink
        ));

        return Result.Success(new IdentifierResponse(user.Id));
    }
}