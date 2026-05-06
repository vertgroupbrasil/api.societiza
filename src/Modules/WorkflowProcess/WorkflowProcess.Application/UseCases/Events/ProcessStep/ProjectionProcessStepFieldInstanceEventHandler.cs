using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowProcess.Domain.Events;
using WorkflowProcess.Domain.Projections;
using WorkflowProcess.Persistence.Projection;

namespace WorkflowProcess.Application.UseCases.Events.ProcessStep;

public interface IProjectionProcessStepFieldInstanceEventHandler :
    IEventHandler<DomainEvents.ProcessStepFieldInstanceMaterialized>,
    IEventHandler<DomainEvents.ProcessStepFieldInstanceFilled> { }

public class ProjectionProcessStepFieldInstanceEventHandler(
    IWorkflowProcessProjection<ProjectionModel.ProcessStepFieldInstance> fieldProjection,
    ILogger<ProjectionProcessStepFieldInstanceEventHandler> logger) : IProjectionProcessStepFieldInstanceEventHandler
{
    public async Task Handle(DomainEvents.ProcessStepFieldInstanceMaterialized @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await fieldProjection.ReplaceInsertAsync(new(
                @event.FieldInstanceId,
                @event.ProcessId,
                @event.StepInstanceId,
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
            logger.LogError(ex, "Failed to project step field instance materialization: {Id}.", @event.FieldInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepFieldInstanceMaterialized)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.ProcessStepFieldInstanceFilled @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await fieldProjection.FindAsync(
                x => x.Id == @event.FieldInstanceId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("ProcessStepFieldInstance projection not found for fill: {Id}.", @event.FieldInstanceId);
                return;
            }

            await fieldProjection.ReplaceInsertAsync(existing with
            {
                Value = @event.Value,
                FilledAt = @event.FilledAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step field instance fill: {Id}.", @event.FieldInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepFieldInstanceFilled)}: {ex.Message}", ex);
        }
    }
}