using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace WorkflowTemplate.Application.UseCases.Events;

public interface IProjectionTemplateStepTaskFieldEventHandler :
    IEventHandler<DomainEvents.WorkflowTemplateStepTaskFieldAdded>,
    IEventHandler<DomainEvents.WorkflowTemplateStepTaskFieldUpdated>,
    IEventHandler<DomainEvents.WorkflowTemplateStepTaskFieldRemoved>;

public class ProjectionTemplateStepTaskFieldEventHandler(
    IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField> workflowProjection,
    ILogger<ProjectionTemplateStepTaskFieldEventHandler> logger) : IProjectionTemplateStepTaskFieldEventHandler
{
    public async Task Handle(DomainEvents.WorkflowTemplateStepTaskFieldAdded @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.ReplaceInsertAsync(new(
                @event.FieldId,
                @event.TemplateId,
                @event.StepId,
                @event.TaskId,
                @event.Label,
                @event.FieldType,
                @event.Options,
                @event.Order,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create template step task field: {Id}.", @event.FieldId);
            throw new InvalidOperationException($"Failed to create template step task field: {@event.FieldId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateStepTaskFieldUpdated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.UpdateFieldsAsync(@event.FieldId, builder => builder
                .Set(x => x.Label, @event.Label)
                .Set(x => x.FieldType, @event.FieldType)
                .Set(x => x.Options, @event.Options)
                .Set(x => x.Order, @event.Order)
                .Set(x => x.UpdatedAt, @event.UpdatedAt), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update template step task field: {Id}.", @event.FieldId);
            throw new InvalidOperationException($"Failed to update template step task field: {@event.FieldId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateStepTaskFieldRemoved @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.DeleteAsync(@event.FieldId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove template step task field: {Id}.", @event.FieldId);
            throw new InvalidOperationException($"Failed to remove template step task field: {@event.FieldId}.", ex);
        }
    }
}