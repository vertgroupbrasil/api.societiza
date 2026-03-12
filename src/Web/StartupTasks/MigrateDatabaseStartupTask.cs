using Accountancy.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using User.Persistence.Context;
using WorkflowProcess.Persistence.Context;
using WorkflowTemplate.Persistence.Context;

namespace Web.StartupTasks;

internal sealed class MigrateDatabaseStartupTask(IServiceProvider provider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = provider.CreateScope();

        await MigrateDatabaseAsync<UserDbContext>(scope, stoppingToken);
        await MigrateDatabaseAsync<AccountancyDbContext>(scope, stoppingToken);
        await MigrateDatabaseAsync<WorkflowProcessDbContext>(scope, stoppingToken);
        await MigrateDatabaseAsync<WorkflowTemplateDbContext>(scope, stoppingToken);
    }

    private static async Task MigrateDatabaseAsync<TDbContext>(IServiceScope scope, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        if (dbContext.Database.GetMigrations().Any())
            await dbContext.Database.MigrateAsync(cancellationToken);
        else
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}