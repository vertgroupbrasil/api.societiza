using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Aggregates;
using Identity.Shared.Commands.Auth;
using Identity.Shared.Response.Auth;

namespace Identity.Application.UseCases.Commands.Auth;

public sealed class RefreshHandler(
    IIdentityApplicationService applicationService,
    ITokenService tokenService
) : ICommandHandler<RefreshCommand, RefreshResponse>
{
    public async Task<Result<RefreshResponse>> Handle(
        RefreshCommand request, CancellationToken cancellationToken)
    {
        var principal = tokenService.ValidateRefreshToken(request.RefreshToken);

        if (principal is null)
            return Result.Failure<RefreshResponse>(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var userIdClaim = principal.FindFirst("sub")?.Value;

        if (userIdClaim is null)
            return Result.Failure<RefreshResponse>(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var userId = Guid.Parse(userIdClaim);

        var userResult = await applicationService.LoadAggregateAsync<User>(userId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure<RefreshResponse>(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        var user = userResult.Value;

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));

        var existingToken = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

        if (existingToken is null || !existingToken.IsValid())
            return Result.Failure<RefreshResponse>(new Error(
                "Auth.InvalidToken", "Refresh token is invalid or expired"));

        user.RevokeRefreshToken(existingToken.Id);

        var newRefreshTokenJwt = tokenService.GenerateRefreshToken(user);
        var newTokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(newRefreshTokenJwt)));

        user.AddRefreshToken(newTokenHash, DateTimeOffset.UtcNow.AddDays(7));

        var accessToken = tokenService.GenerateAccessToken(user);

        await applicationService.AppendEventsAsync(user, cancellationToken);

        return Result.Success(new RefreshResponse(accessToken, newRefreshTokenJwt));
    }
}
