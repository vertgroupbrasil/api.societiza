using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Aggregates;
using Identity.Shared.Commands.Auth;

namespace Identity.Application.UseCases.Commands.Auth;

public sealed class LogoutHandler(
    IIdentityApplicationService applicationService,
    ITokenService tokenService
) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var principal = tokenService.ValidateRefreshToken(request.RefreshToken);

        if (principal is null)
            return Result.Failure(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var userIdClaim = principal.FindFirst("sub")?.Value;

        if (userIdClaim is null)
            return Result.Failure(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var userId = Guid.Parse(userIdClaim);

        var userResult = await applicationService.LoadAggregateAsync<User>(userId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var user = userResult.Value;

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));

        var existingToken = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        if (existingToken is null || !existingToken.IsValid())
            return Result.Failure(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        user.RevokeRefreshToken(existingToken.Id);

        await applicationService.AppendEventsAsync(user, cancellationToken);

        return Result.Success();
    }
}
