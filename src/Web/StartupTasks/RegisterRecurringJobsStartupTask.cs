using Core.Application.Services.Interfaces;
using Identity.Application.UseCases.Jobs;

namespace Web.StartupTasks;

internal sealed class RegisterRecurringJobsStartupTask(IServiceProvider provider) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = provider.CreateScope();
        var jobScheduler = scope.ServiceProvider.GetRequiredService<IJobSchedulerService>();

        jobScheduler.ScheduleRecurring("expire-pending-invites", "0 * * * *", new ExpireInvitesCommand());

        return Task.CompletedTask;
    }
}