using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace WorkflowTemplate.Application.UseCases.Events.Template;

public interface IProjectionWorkflowTemplateEventHandler :
    IEventHandler<DomainEvents.WorkflowTemplateCreated>,
    IEventHandler<DomainEvents.WorkflowTemplateUpdated>,
    IEventHandler<DomainEvents.WorkflowTemplateActivated>,
    IEventHandler<DomainEvents.WorkflowTemplateArchived>,
    IEventHandler<DomainEvents.WorkflowTemplateDeleted>;

public class ProjectionWorkflowTemplateEventHandler(
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate> workflowProjection,
    ILogger<ProjectionWorkflowTemplateEventHandler> logger) : IProjectionWorkflowTemplateEventHandler
{
    public async Task Handle(DomainEvents.WorkflowTemplateCreated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.ReplaceInsertAsync(new(
                @event.Id,
                @event.Name,
                @event.Description,
                @event.TemplateVersion,
                @event.Status,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create workflow template: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to create workflow template: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateUpdated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.UpdateFieldsAsync(@event.Id, builder => builder
                .Set(x => x.Name, @event.Name)
                .Set(x => x.Description, @event.Description)
                .Set(x => x.UpdatedAt, @event.UpdatedAt), 
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update workflow template: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to update workflow template: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateActivated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.UpdateFieldsAsync(@event.Id, builder => builder
                .Set(x => x.Status, TemplateStatus.Active.Name)
                .Set(x => x.UpdatedAt, @event.UpdatedAt), 
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to activate workflow template: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to activate workflow template: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateArchived @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.UpdateFieldsAsync(@event.Id, builder => builder
                .Set(x => x.Status, TemplateStatus.Archived.Name)
                .Set(x => x.UpdatedAt, @event.UpdatedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to archive workflow template: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to archive workflow template: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowTemplateDeleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProjection.UpdateFieldsAsync(@event.Id, builder => builder
                .Set(x => x.IsDeleted, true),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete workflow template: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to delete workflow template: {@event.Id}.", ex);
        }
    }
}