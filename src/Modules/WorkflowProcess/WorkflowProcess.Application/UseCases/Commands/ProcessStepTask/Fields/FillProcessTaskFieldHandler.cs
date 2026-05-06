using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Shared.Results;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Shared.Commands.ProcessStepTask.Fields;
using WorkflowProcess.Shared.Errors;

namespace WorkflowProcess.Application.UseCases.Commands.ProcessStepTask.Fields;

public sealed class FillProcessTaskFieldHandler(
    IWorkflowProcessApplicationService applicationService,
    ICurrentUserService currentUser
) : ICommandHandler<FillProcessTaskFieldCommand>
{
    public async Task<Result> Handle(FillProcessTaskFieldCommand request, CancellationToken cancellationToken)
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

        var currentStep = process.Steps
            .OrderBy(s => s.Order)
            .First(s => s.Status != StepInstanceStatus.Completed);

        if (currentStep.Id != request.StepInstanceId)
            return Result.Failure(WorkflowProcessErrors.StepNotUnlocked);

        var field = task.Fields.FirstOrDefault(f => f.Id == request.FieldInstanceId);
        if (field is null)
            return Result.Failure(WorkflowProcessErrors.TaskFieldNotFound);

        process.FillTaskField(request.StepInstanceId, request.TaskInstanceId, request.FieldInstanceId, request.Value);

        await applicationService.AppendEventsAsync(process, cancellationToken);
        return Result.Success();
    }
}
