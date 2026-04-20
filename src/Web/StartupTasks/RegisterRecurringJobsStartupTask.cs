using Core.Application.Services.Interfaces;
using Identity.Shared.Jobs;

namespace Web.StartupTasks;

internal sealed class RegisterRecurringJobsStartupTask(IServiceProvider provider) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = provider.CreateScope();
        var jobScheduler = scope.ServiceProvider.GetRequiredService<IJobSchedulerService>();

        jobScheduler.ScheduleRecurring("expire-pending-invitation-links", "0 * * * *", new ExpireInvitationLinksCommand());

        return Task.CompletedTask;
    }
}