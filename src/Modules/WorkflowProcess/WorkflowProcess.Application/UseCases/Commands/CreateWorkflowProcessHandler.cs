using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Shared.Commands.Process;
using WorkflowProcess.Shared.Errors;

namespace WorkflowProcess.Application.UseCases.Commands;

using WorkflowProcessAggregate = Domain.Aggregates.WorkflowProcess;

public sealed class CreateWorkflowProcessHandler(
    IWorkflowProcessApplicationService applicationService,
    IWorkflowTemplateDataService templateDataService
) : ICommandHandler<CreateWorkflowProcessCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(
        CreateWorkflowProcessCommand request, CancellationToken cancellationToken)
    {
        var template = await templateDataService.GetActiveTemplateSnapshotAsync(request.TemplateId, cancellationToken);

        if (template is null)
            return Result.Failure<IdentifierResponse>(WorkflowProcessErrors.TemplateNotActiveOrNotFound);

        var process = WorkflowProcessAggregate.Create(
            accountancyId: request.AccountancyId,
            templateId: request.TemplateId,
            templateVersion: template.Value.Version,
            processType: request.ProcessType,
            targetClient: request.TargetClient);

        foreach (var step in template.Value.Steps)
        {
            var stepInstanceId = process.MaterializeStep(
                templateStepId: step.StepId,
                title: step.Title,
                order: step.Order);

            foreach (var field in step.Fields)
            {
                process.MaterializeStepField(
                    stepInstanceId: stepInstanceId,
                    templateFieldId: field.FieldId,
                    label: field.Label,
                    fieldType: field.FieldType,
                    options: field.Options,
                    isRequired: false,
                    order: field.Order);
            }

            foreach (var task in step.Tasks)
            {
                var taskInstanceId = process.MaterializeTask(
                    stepInstanceId: stepInstanceId,
                    templateTaskId: task.TaskId,
                    title: task.Title,
                    order: task.Order,
                    isOptional: task.IsOptional);

                foreach (var taskField in task.Fields)
                {
                    process.MaterializeTaskField(
                        stepInstanceId: stepInstanceId,
                        taskInstanceId: taskInstanceId,
                        templateFieldId: taskField.FieldId,
                        label: taskField.Label,
                        fieldType: taskField.FieldType,
                        options: taskField.Options,
                        isRequired: false,
                        order: taskField.Order);
                }
            }
        }

        await applicationService.AppendEventsAsync(process, cancellationToken);
        return Result.Success(new IdentifierResponse(process.Id));
    }
}