using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Options;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands.Auth;
using Identity.Shared.Emails;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.Commands.Auth;

using UserAggregate = Identity.Domain.Aggregates.User;

public sealed class ForgotPasswordHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.User> userProjection,
    IJobSchedulerService jobScheduler,
    IOptions<FrontendOptions> frontendOptions
) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var userProjectionModel = await userProjection.FindAsync(x => x.Email == request.Email, cancellationToken);

        if (userProjectionModel is null)
            return Result.Success();

        var userResult = await applicationService.LoadAggregateAsync<UserAggregate>(userProjectionModel.Id, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Success();

        var user = userResult.Value;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        user.AddPasswordResetToken(tokenHash);
        await applicationService.AppendEventsAsync(user, cancellationToken);

        var resetLink = $"{frontendOptions.Value.BaseUrl}/auth/reset-password?token={Uri.EscapeDataString(rawToken)}";

        jobScheduler.Enqueue(new SendPasswordResetEmailCommand(
            ToEmail: request.Email,
            ToName: request.Email,
            ResetLink: resetLink
        ));

        return Result.Success();
    }
}