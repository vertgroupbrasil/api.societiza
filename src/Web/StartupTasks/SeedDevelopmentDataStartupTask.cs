using Identity.Application.Services;
using Identity.Application.Services.Abstractions;
using Identity.Domain.Aggregates;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;

namespace Web.StartupTasks;

internal sealed class SeedDevelopmentDataStartupTask(
    IServiceProvider provider,
    DatabaseMigrationSignal migrationSignal,
    ILogger<SeedDevelopmentDataStartupTask> logger
) : BackgroundService
{
    private const string AdminEmail    = "admin@societiza.dev";
    private const string AdminPassword = "Admin@123";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await migrationSignal.WaitAsync(stoppingToken);

        using IServiceScope scope = provider.CreateScope();

        var userProjection = scope.ServiceProvider
            .GetRequiredService<IIdentityProjection<ProjectionModel.User>>();

        var anyUser = await userProjection.FindAsync(u => !u.IsDeleted, stoppingToken);

        if (anyUser is not null)
            return;

        var passwordService = scope.ServiceProvider
            .GetRequiredService<IPasswordService>();

        var applicationService = scope.ServiceProvider
            .GetRequiredService<IIdentityApplicationService>();

        var user = User.Create(
            invitationLinkId: Guid.Empty,
            firstName: "Admin",
            lastName: "Societiza",
            email: AdminEmail,
            passwordHash: passwordService.HashPassword(AdminPassword),
            role: UserRole.SystemAdmin,
            accountancyId: null
        );

        await applicationService.AppendEventsAsync(user, stoppingToken);

        logger.LogWarning(
            "DEV SEED — SystemAdmin created. Email: {Email} | Password: {Password}",
            AdminEmail, AdminPassword);
    }
}