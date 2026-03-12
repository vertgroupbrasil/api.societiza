using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace WorkflowTemplate.Application.UseCases.Events;

public interface IProjectionTemplateStepFieldEventHandler :
    IEventHandler<DomainEvents.WorkflowTemplateStepFieldAdded>,
    IEventHandler<DomainEvents.WorkflowTemplateStepFieldUpdated>,
    IEventHandler<DomainEvents.WorkflowTemplateStepFieldRemoved>;

public class ProjectionTemplateStepFieldEventHandler(
    IWorkflowTemplateProjection<ProjectionModel.TemplateStepField> workflowProjection,
    ILogger<ProjectionTemplateStepFieldEventHandler> logger) : IProjectionTemplateStepFieldEventHandler
{
    public async Task Handle(DomainEvents.WorkflowTemplateStepFieldAdded @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.ReplaceInsertAsync(new(
                @event.FieldId,
                @event.TemplateId,
                @event.StepId,
                @event.Label,
                @event.FieldType,
                @event.Options,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create template step field: {Id}.", @event.FieldId);
            throw new InvalidOperationException($"Failed to create template step field: {@event.FieldId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateStepFieldUpdated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.UpdateFieldsAsync(@event.FieldId, builder => builder
                .Set(x => x.Label, @event.Label)
                .Set(x => x.FieldType, @event.FieldType)
                .Set(x => x.Options, @event.Options)
                .Set(x => x.UpdatedAt, @event.UpdatedAt), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update template step field: {Id}.", @event.FieldId);
            throw new InvalidOperationException($"Failed to update template step field: {@event.FieldId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateStepFieldRemoved @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.DeleteAsync(@event.FieldId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove template step field: {Id}.", @event.FieldId);
            throw new InvalidOperationException($"Failed to remove template step field: {@event.FieldId}.", ex);
        }
    }
}
