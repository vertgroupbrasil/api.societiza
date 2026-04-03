using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.Template;

public class CreateWorkflowTemplateDraftHandler(
    IWorkflowTemplateApplicationService applicationService,
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate> templateProjection)
    : ICommandHandler<CreateWorkflowTemplateDraftCommand, IdentifierResponse>
{
    private static string BuildCloneSeed(string prefix, int order)
        => $"__clone__{prefix}_{order}_{Guid.NewGuid():N}";

    public async Task<Result<IdentifierResponse>> Handle(
        CreateWorkflowTemplateDraftCommand request,
        CancellationToken cancellationToken)
    {
        var existingDraft = await templateProjection.FindAsync(
            template => template.SourceTemplateId == request.Id && template.Status == "Draft" && !template.IsDeleted,
            cancellationToken);

        if (existingDraft is not null)
            return Result.Success(new IdentifierResponse(existingDraft.Id));

        var sourceResult = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(
            request.Id,
            cancellationToken);

        if (!sourceResult.IsSuccess)
            return Result.Failure<IdentifierResponse>(sourceResult.Error);

        var source = sourceResult.Value;
        var draft = WorkflowTemplateAggregate.Create(
            source.Name,
            source.Description,
            source.Id);

        foreach (var step in source.Steps.OrderBy(step => step.Order))
        {
            // Some existing templates already contain duplicated step or task titles.
            // The aggregate still guards AddStep/AddTask against duplicates, so we
            // create with a unique temporary title and then update back to the
            // original title to preserve the current structure when cloning.
            var draftStepId = draft.AddStep(
                BuildCloneSeed("step", step.Order),
                step.Description,
                step.Order);

            draft.UpdateStep(draftStepId, step.Title, step.Description, step.Order);

            foreach (var field in step.Fields.OrderBy(field => field.Order))
                draft.AddStepField(
                    draftStepId,
                    field.Label,
                    field.FieldType,
                    field.Options,
                    field.Order);

            foreach (var task in step.Tasks.OrderBy(task => task.Order))
            {
                var draftTaskId = draft.AddTask(
                    draftStepId,
                    BuildCloneSeed("task", task.Order),
                    task.Description,
                    task.Type,
                    task.Configuration,
                    task.Order,
                    task.IsOptional);

                draft.UpdateTask(
                    draftStepId,
                    draftTaskId,
                    task.Title,
                    task.Description,
                    task.Type,
                    task.Configuration,
                    task.Order,
                    task.IsOptional);

                foreach (var field in task.Fields.OrderBy(field => field.Order))
                    draft.AddTaskField(
                        draftStepId,
                        draftTaskId,
                        field.Label,
                        field.FieldType,
                        field.Options,
                        field.Order);
            }
        }

        await applicationService.AppendEventsAsync(draft, cancellationToken);
        return Result.Success(new IdentifierResponse(draft.Id));
    }
}
