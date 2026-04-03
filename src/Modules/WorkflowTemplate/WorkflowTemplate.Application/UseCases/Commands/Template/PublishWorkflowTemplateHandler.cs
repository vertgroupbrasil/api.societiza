using Core.Application.Messaging.Commands;
using Core.Shared.Response;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Application.UseCases.Commands.Template;

public class PublishWorkflowTemplateHandler(
    IWorkflowTemplateApplicationService applicationService)
    : ICommandHandler<PublishWorkflowTemplateCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(
        PublishWorkflowTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var draftResult = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(
            request.Id,
            cancellationToken);

        if (!draftResult.IsSuccess)
            return Result.Failure<IdentifierResponse>(draftResult.Error);

        var draft = draftResult.Value;

        if (!draft.SourceTemplateId.HasValue)
        {
            // Publishing a standalone template only needs to ensure it is active.
            if (draft.Status != WorkflowTemplate.Domain.Enumerations.TemplateStatus.Active)
                draft.Activate();

            await applicationService.AppendEventsAsync(draft, cancellationToken);
            return Result.Success(new IdentifierResponse(draft.Id));
        }

        var sourceResult = await applicationService.LoadAggregateAsync<WorkflowTemplateAggregate>(
            draft.SourceTemplateId.Value,
            cancellationToken);

        if (!sourceResult.IsSuccess)
            return Result.Failure<IdentifierResponse>(sourceResult.Error);

        var source = sourceResult.Value;
        ValidateDraftStructure(draft);

        // The source template keeps its identity.
        // Publish replaces the structure, not the aggregate identity.
        source.Update(draft.Name, draft.Description);

        // Remove the current structure before recreating it in the order defined by the draft.
        foreach (var stepId in source.Steps.Select(step => step.Id).ToList())
            source.RemoveStep(stepId);

        foreach (var step in draft.Steps.OrderBy(step => step.Order))
        {
            var sourceStepId = source.AddStep(step.Title, step.Description, step.Order);

            foreach (var field in step.Fields.OrderBy(field => field.Order))
                source.AddStepField(
                    sourceStepId,
                    field.Label,
                    field.FieldType,
                    field.Options,
                    field.Order);

            foreach (var task in step.Tasks.OrderBy(task => task.Order))
            {
                var sourceTaskId = source.AddTask(
                    sourceStepId,
                    task.Title,
                    task.Description,
                    task.Type,
                    task.Configuration,
                    task.Order,
                    task.IsOptional);

                foreach (var field in task.Fields.OrderBy(field => field.Order))
                    source.AddTaskField(
                        sourceStepId,
                        sourceTaskId,
                        field.Label,
                        field.FieldType,
                        field.Options,
                        field.Order);
            }
        }

        if (source.Status != WorkflowTemplate.Domain.Enumerations.TemplateStatus.Active)
            source.Activate();

        if (draft.Status != WorkflowTemplate.Domain.Enumerations.TemplateStatus.Archived)
            draft.Archive();

        // Persist source first so the active template reflects the new structure,
        // then archive the draft so the edit copy cannot be published twice.
        await applicationService.AppendEventsAsync(source, cancellationToken);
        await applicationService.AppendEventsAsync(draft, cancellationToken);

        return Result.Success(new IdentifierResponse(source.Id));
    }

    private static void ValidateDraftStructure(WorkflowTemplateAggregate draft)
    {
        // Duplicate titles are rejected here so publish returns a meaningful error
        // instead of failing later inside the aggregate with a less actionable conflict.
        var duplicateStepTitles = draft.Steps
            .GroupBy(step => step.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group => group.First().Title.Trim())
            .ToArray();

        if (duplicateStepTitles.Length > 0)
            throw new ArgumentException(
                $"Duplicate step titles detected: {string.Join(", ", duplicateStepTitles)}. Each workflow step must have a unique name before publishing.");

        var duplicateTaskTitlesByStep = draft.Steps
            .Select(step => new
            {
                StepTitle = step.Title,
                Duplicates = step.Tasks
                    .GroupBy(task => task.Title.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                    .Select(group => group.First().Title.Trim())
                    .ToArray()
            })
            .Where(item => item.Duplicates.Length > 0)
            .ToArray();

        if (duplicateTaskTitlesByStep.Length == 0) return;

        var detail = string.Join(
            "; ",
            duplicateTaskTitlesByStep.Select(item =>
                $"{item.StepTitle}: {string.Join(", ", item.Duplicates)}"));

        throw new ArgumentException(
            $"Duplicate task titles detected inside the same step: {detail}. Each task must have a unique name inside its step before publishing.");
    }
}
