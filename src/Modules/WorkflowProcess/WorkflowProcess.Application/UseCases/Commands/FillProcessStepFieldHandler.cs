using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Shared.Commands.ProcessStep.Fields;
using WorkflowProcess.Shared.Errors;

namespace WorkflowProcess.Application.UseCases.Commands;

public sealed class FillProcessStepFieldHandler(
    IWorkflowProcessApplicationService applicationService
) : ICommandHandler<FillProcessStepFieldCommand>
{
    public async Task<Result> Handle(FillProcessStepFieldCommand request, CancellationToken cancellationToken)
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

        var field = step.Fields.FirstOrDefault(f => f.Id == request.FieldInstanceId);
        if (field is null)
            return Result.Failure(WorkflowProcessErrors.StepFieldNotFound);

        process.FillStepField(request.StepInstanceId, request.FieldInstanceId, request.Value);

        await applicationService.AppendEventsAsync(process, cancellationToken);

        return Result.Success();
    }
}
