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
using Identity.Shared.Response.Auth;

namespace Identity.Application.UseCases.Commands.Auth;

using UserAggregate = Identity.Domain.Aggregates.User;

public sealed class LoginHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.User> userProjection,
    IPasswordService passwordService,
    ITokenService tokenService
) : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var userProjectionModel = await userProjection.FindAsync(x => x.Email == request.Email, cancellationToken);

        if (userProjectionModel is null)
            return Result.Failure<LoginResponse>(new Error(
                "Auth.InvalidCredentials", "Invalid email or password"));

        var userResult = await applicationService.LoadAggregateAsync<UserAggregate>(userProjectionModel.Id, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure<LoginResponse>(new Error(
                "Auth.InvalidCredentials", "Invalid email or password"));

        var user = userResult.Value;

        if (!passwordService.VerifyPassword(request.Password, user.PasswordHash!))
            return Result.Failure<LoginResponse>(new Error(
                "Auth.InvalidCredentials", "Invalid email or password"));

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshTokenJwt = tokenService.GenerateRefreshToken(user);
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshTokenJwt)));

        user.AddRefreshToken(tokenHash, DateTimeOffset.UtcNow.AddDays(7));

        await applicationService.AppendEventsAsync(user, cancellationToken);
        return Result.Success(new LoginResponse(accessToken, refreshTokenJwt));
    }
}