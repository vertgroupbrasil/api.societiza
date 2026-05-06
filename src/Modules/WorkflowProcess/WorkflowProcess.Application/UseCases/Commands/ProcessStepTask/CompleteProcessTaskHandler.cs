using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Shared.Results;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Shared.Commands.ProcessStepTask;
using WorkflowProcess.Shared.Errors;

namespace WorkflowProcess.Application.UseCases.Commands.ProcessStepTask;

public sealed class CompleteProcessTaskHandler(
    IWorkflowProcessApplicationService applicationService,
    ICurrentUserService currentUser
) : ICommandHandler<CompleteProcessTaskCommand>
{
    public async Task<Result> Handle(CompleteProcessTaskCommand request, CancellationToken cancellationToken)
    {
        var processResult = await applicationService.LoadAggregateAsync<Domain.Aggregates.WorkflowProcess>(request.ProcessId, cancellationToken);

        if (!processResult.IsSuccess)
            return Result.Failure(WorkflowProcessErrors.ProcessNotFound);

        var process = processResult.Value;

        if (currentUser.Role != IdentityRoles.SystemAdmin && currentUser.AccountancyId != process.AccountancyId)
            return Result.Failure(WorkflowProcessErrors.Forbidden);

        if (process.Status == ProcessStatus.Completed)
            return Result.Failure(WorkflowProcessErrors.ProcessAlreadyCompleted);

        var step = process.Steps.FirstOrDefault(s => s.Id == request.StepInstanceId);
        if (step is null)
            return Result.Failure(WorkflowProcessErrors.StepNotFound);

        var task = step.Tasks.FirstOrDefault(t => t.Id == request.TaskInstanceId);
        if (task is null)
            return Result.Failure(WorkflowProcessErrors.TaskNotFound);

        if (task.Status != TaskInstanceStatus.Pending)
            return Result.Failure(WorkflowProcessErrors.TaskAlreadyResolved);

        var currentStep = process.Steps
            .OrderBy(s => s.Order)
            .First(s => s.Status != StepInstanceStatus.Completed);

        if (currentStep.Id != request.StepInstanceId)
            return Result.Failure(WorkflowProcessErrors.StepNotUnlocked);

        var blockingTask = step.Tasks
            .Where(t => !t.IsOptional && t.Order < task.Order && t.Status == TaskInstanceStatus.Pending)
            .OrderBy(t => t.Order)
            .FirstOrDefault();

        if (blockingTask is not null)
            return Result.Failure(WorkflowProcessErrors.PreviousRequiredTaskNotResolved(blockingTask.Title));

        process.CompleteTask(request.StepInstanceId, request.TaskInstanceId);

        await applicationService.AppendEventsAsync(process, cancellationToken);
        return Result.Success();
    }
}
