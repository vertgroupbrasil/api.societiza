using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Commands.Auth;

using UserAggregate = Identity.Domain.Aggregates.User;

public sealed class ResetPasswordHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.PasswordResetToken> passwordResetTokenProjection,
    IPasswordService passwordService
) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var tokenProjectionModel = await passwordResetTokenProjection.FindAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (tokenProjectionModel is null)
            return Result.Failure(new NotFoundError(new(
                "Auth.InvalidToken", "Password reset token is invalid or expired")));

        var userResult = await applicationService.LoadAggregateAsync<UserAggregate>(tokenProjectionModel.UserId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure(userResult.Error);

        var user = userResult.Value;
        var resetToken = user.PasswordResetTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        if (resetToken is null || !resetToken.IsValid())
            return Result.Failure(new NotFoundError(new(
                "Auth.InvalidToken", "Password reset token is invalid or expired")));

        user.UsePasswordResetToken(resetToken.Id);

        var newPasswordHash = passwordService.HashPassword(request.NewPassword);

        user.UpdatePassword(newPasswordHash);
        user.RevokeAllRefreshTokens();
        user.RegenerateSecurityStamp();

        await applicationService.AppendEventsAsync(user, cancellationToken);
        return Result.Success();
    }
}