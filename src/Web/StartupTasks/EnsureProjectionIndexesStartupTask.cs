using Identity.Persistence.Context.Interfaces;

namespace Web.StartupTasks;

internal sealed class EnsureProjectionIndexesStartupTask(IServiceProvider provider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IIdentityProjectionDbContext>();

        await dbContext.EnsureIndexesAsync(stoppingToken);
    }
}
