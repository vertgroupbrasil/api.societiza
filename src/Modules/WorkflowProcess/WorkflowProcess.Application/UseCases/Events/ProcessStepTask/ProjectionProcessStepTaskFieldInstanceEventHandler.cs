using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowProcess.Domain.Events;
using WorkflowProcess.Domain.Projections;
using WorkflowProcess.Persistence.Projection;

namespace WorkflowProcess.Application.UseCases.Events.ProcessStepTask;

public interface IProjectionProcessStepTaskFieldInstanceEventHandler :
    IEventHandler<DomainEvents.ProcessStepTaskFieldInstanceMaterialized>,
    IEventHandler<DomainEvents.ProcessStepTaskFieldInstanceFilled> { }

public class ProjectionProcessStepTaskFieldInstanceEventHandler(
    IWorkflowProcessProjection<ProjectionModel.ProcessStepTaskFieldInstance> taskFieldProjection,
    ILogger<ProjectionProcessStepTaskFieldInstanceEventHandler> logger) : IProjectionProcessStepTaskFieldInstanceEventHandler
{
    public async Task Handle(DomainEvents.ProcessStepTaskFieldInstanceMaterialized @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await taskFieldProjection.ReplaceInsertAsync(new(
                @event.FieldInstanceId,
                @event.ProcessId,
                @event.StepInstanceId,
                @event.TaskInstanceId,
                @event.TemplateFieldId,
                @event.Label,
                @event.FieldType,
                @event.Options,
                @event.Order,
                null,   // Value
                null,   // FilledAt
                @event.CreatedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step task field instance materialization: {Id}.", @event.FieldInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepTaskFieldInstanceMaterialized)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.ProcessStepTaskFieldInstanceFilled @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await taskFieldProjection.FindAsync(
                x => x.Id == @event.FieldInstanceId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("ProcessStepTaskFieldInstance projection not found for fill: {Id}.", @event.FieldInstanceId);
                return;
            }

            await taskFieldProjection.ReplaceInsertAsync(existing with
            {
                Value = @event.Value,
                FilledAt = @event.FilledAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step task field instance fill: {Id}.", @event.FieldInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepTaskFieldInstanceFilled)}: {ex.Message}", ex);
        }
    }
}