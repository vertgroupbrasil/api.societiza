using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Shared.Results;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Shared.Commands.ProcessStepTask;
using WorkflowProcess.Shared.Errors;

namespace WorkflowProcess.Application.UseCases.Commands.ProcessStepTask;

public sealed class RevertProcessTaskHandler(
    IWorkflowProcessApplicationService applicationService,
    ICurrentUserService currentUser
) : ICommandHandler<RevertProcessTaskCommand>
{
    public async Task<Result> Handle(RevertProcessTaskCommand request, CancellationToken cancellationToken)
    {
        var processResult = await applicationService.LoadAggregateAsync<Domain.Aggregates.WorkflowProcess>(request.ProcessId, cancellationToken);

        if (!processResult.IsSuccess)
            return Result.Failure(WorkflowProcessErrors.ProcessNotFound);

        var process = processResult.Value;

        if (currentUser.Role != IdentityRoles.SystemAdmin && currentUser.AccountancyId != process.AccountancyId)
            return Result.Failure(WorkflowProcessErrors.Forbidden);

        var step = process.Steps.FirstOrDefault(s => s.Id == request.StepInstanceId);
        if (step is null)
            return Result.Failure(WorkflowProcessErrors.StepNotFound);

        var task = step.Tasks.FirstOrDefault(t => t.Id == request.TaskInstanceId);
        if (task is null)
            return Result.Failure(WorkflowProcessErrors.TaskNotFound);

        if (task.Status == TaskInstanceStatus.Pending)
            return Result.Failure(WorkflowProcessErrors.TaskNotRevokable);

        var downstreamStep = process.Steps
            .Where(s => s.Order > step.Order && s.Status != StepInstanceStatus.NotStarted)
            .OrderBy(s => s.Order)
            .FirstOrDefault();

        if (downstreamStep is not null)
            return Result.Failure(WorkflowProcessErrors.DownstreamStepHasProgress(downstreamStep.Title));

        process.RevertTask(request.StepInstanceId, request.TaskInstanceId);

        await applicationService.AppendEventsAsync(process, cancellationToken);
        return Result.Success();
    }
}
