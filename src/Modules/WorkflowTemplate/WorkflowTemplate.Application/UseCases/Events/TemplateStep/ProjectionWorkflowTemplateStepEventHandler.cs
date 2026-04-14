using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace WorkflowTemplate.Application.UseCases.Events.TemplateStep;

public interface IProjectionWorkflowTemplateStepEventHandler :
    IEventHandler<DomainEvents.WorkflowTemplateStepAdded>,
    IEventHandler<DomainEvents.WorkflowTemplateStepUpdated>,
    IEventHandler<DomainEvents.WorkflowTemplateStepRemoved>;

public class ProjectionWorkflowTemplateStepEventHandler(
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStep> stepProjection,
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask> taskProjection,
    IWorkflowTemplateProjection<ProjectionModel.TemplateStepField> stepFieldProjection,
    IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField> taskFieldProjection,
    ILogger<ProjectionWorkflowTemplateStepEventHandler> logger) : IProjectionWorkflowTemplateStepEventHandler
{
    public async Task Handle(DomainEvents.WorkflowTemplateStepAdded @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await stepProjection.ReplaceInsertAsync(new(
                @event.StepId,
                @event.TemplateId,
                @event.Title,
                @event.Description,
                @event.Order,
                @event.AppliesTo,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create workflow template step: {Id}.", @event.StepId);
            throw new InvalidOperationException($"Failed to create workflow template step: {@event.StepId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateStepUpdated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await stepProjection.UpdateFieldsAsync(@event.StepId, builder => builder
                .Set(x => x.Title, @event.Title)
                .Set(x => x.Description, @event.Description)
                .Set(x => x.Order, @event.Order)
                .Set(x => x.AppliesTo, @event.AppliesTo)
                .Set(x => x.UpdatedAt, @event.UpdatedAt), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update workflow template step: {Id}.", @event.StepId);
            throw new InvalidOperationException($"Failed to update workflow template step: {@event.StepId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateStepRemoved @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await stepProjection.DeleteAsync(x => x.Id == @event.StepId, cancellationToken);

            if (@event.FieldIds.Count > 0)
                await stepFieldProjection.DeleteAsync(x => @event.FieldIds.Contains(x.Id), cancellationToken);

            if (@event.TaskIds.Count > 0)
                await taskProjection.DeleteAsync(x => @event.TaskIds.Contains(x.Id), cancellationToken);

            if (@event.TaskFieldIds.Count > 0)
                await taskFieldProjection.DeleteAsync(x => @event.TaskFieldIds.Contains(x.Id), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove workflow template step: {Id}.", @event.StepId);
            throw new InvalidOperationException($"Failed to remove workflow template step: {@event.StepId}.", ex);
        }
    }
}