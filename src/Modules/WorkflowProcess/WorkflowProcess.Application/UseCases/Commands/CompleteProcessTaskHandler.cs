using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Shared.Commands.ProcessTask;
using WorkflowProcess.Shared.Errors;

namespace WorkflowProcess.Application.UseCases.Commands;

public sealed class CompleteProcessTaskHandler(
    IWorkflowProcessApplicationService applicationService
) : ICommandHandler<CompleteProcessTaskCommand>
{
    public async Task<Result> Handle(CompleteProcessTaskCommand request, CancellationToken cancellationToken)
    {
        var processResult = await applicationService.LoadAggregateAsync<Domain.Aggregates.WorkflowProcess>(
            request.ProcessId, cancellationToken);

        if (!processResult.IsSuccess)
            return Result.Failure(WorkflowProcessErrors.ProcessNotFound);

        var process = processResult.Value;

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

        process.CompleteTask(request.StepInstanceId, request.TaskInstanceId);

        await applicationService.AppendEventsAsync(process, cancellationToken);
        return Result.Success();
    }
}