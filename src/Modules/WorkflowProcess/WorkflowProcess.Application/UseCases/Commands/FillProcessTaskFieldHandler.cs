using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Shared.Commands.ProcessTask.Fields;
using WorkflowProcess.Shared.Errors;

namespace WorkflowProcess.Application.UseCases.Commands;

public sealed class FillProcessTaskFieldHandler(
    IWorkflowProcessApplicationService applicationService
) : ICommandHandler<FillProcessTaskFieldCommand>
{
    public async Task<Result> Handle(FillProcessTaskFieldCommand request, CancellationToken cancellationToken)
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

        var field = task.Fields.FirstOrDefault(f => f.Id == request.FieldInstanceId);
        if (field is null)
            return Result.Failure(WorkflowProcessErrors.TaskFieldNotFound);

        process.FillTaskField(request.StepInstanceId, request.TaskInstanceId, request.FieldInstanceId, request.Value);

        await applicationService.AppendEventsAsync(process, cancellationToken);
        return Result.Success();
    }
}