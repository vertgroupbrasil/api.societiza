using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Enumerations;
using Identity.Shared.Commands;

namespace Identity.Application.UseCases.Commands;

internal sealed class AcceptInviteHandler(
    IIdentityApplicationService applicationService,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService
) : ICommandHandler<AcceptInviteCommand>
{
    public async Task<Result> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        var tokenClaims = jwtTokenService.ValidatePasswordResetToken(request.Token);

        if (tokenClaims is null)
            return Result.Failure(new Error("Token.Invalid", "Invalid or expired token"));

        var userResult = await applicationService.LoadAggregateAsync<User>(tokenClaims.Value.UserId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure(userResult.Error);

        if (userResult.Value.Status != UserStatus.PendingInvite)
            return Result.Failure(new Error("Token.Expired", "Token has already been used or is no longer valid"));

        var passwordHash = passwordService.HashPassword(request.Password);
        var completeResult = userResult.Value.CompleteRegistration(passwordHash);

        if (!completeResult.IsSuccess)
            return completeResult;

        await applicationService.AppendEventsAsync(userResult.Value, cancellationToken);
        return Result.Success();
    }
}