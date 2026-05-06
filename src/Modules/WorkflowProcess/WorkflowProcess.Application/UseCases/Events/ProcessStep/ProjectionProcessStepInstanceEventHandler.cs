using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Domain.Events;
using WorkflowProcess.Domain.Projections;
using WorkflowProcess.Persistence.Projection;

namespace WorkflowProcess.Application.UseCases.Events.ProcessStep;

public interface IProjectionProcessStepInstanceEventHandler :
    IEventHandler<DomainEvents.ProcessStepInstanceMaterialized>,
    IEventHandler<DomainEvents.ProcessStepInstanceStarted>,
    IEventHandler<DomainEvents.ProcessStepInstanceCompleted>,
    IEventHandler<DomainEvents.ProcessStepInstanceReverted> { }

public class ProjectionProcessStepInstanceEventHandler(
    IWorkflowProcessProjection<ProjectionModel.ProcessStepInstance> stepProjection,
    ILogger<ProjectionProcessStepInstanceEventHandler> logger) : IProjectionProcessStepInstanceEventHandler
{
    public async Task Handle(DomainEvents.ProcessStepInstanceReverted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await stepProjection.FindAsync(
                x => x.Id == @event.StepInstanceId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("ProcessStepInstance projection not found for revert: {Id}.", @event.StepInstanceId);
                return;
            }

            await stepProjection.ReplaceInsertAsync(existing with
            {
                Status = @event.NewStatus,
                CompletedAt = null,
                UpdatedAt = @event.RevertedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step instance revert: {Id}.", @event.StepInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepInstanceReverted)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.ProcessStepInstanceMaterialized @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await stepProjection.ReplaceInsertAsync(new(
                @event.StepInstanceId,
                @event.ProcessId,
                @event.TemplateStepId,
                @event.Title,
                @event.Order,
                @event.Status,
                @event.CreatedAt,
                null,   // CompletedAt
                null),  // UpdatedAt
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step instance materialization: {Id}.", @event.StepInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepInstanceMaterialized)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.ProcessStepInstanceStarted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await stepProjection.FindAsync(
                x => x.Id == @event.StepInstanceId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("ProcessStepInstance projection not found for start: {Id}.", @event.StepInstanceId);
                return;
            }

            await stepProjection.ReplaceInsertAsync(existing with
            {
                Status = StepInstanceStatus.InProgress.Name,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step instance start: {Id}.", @event.StepInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepInstanceStarted)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.ProcessStepInstanceCompleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await stepProjection.FindAsync(
                x => x.Id == @event.StepInstanceId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("ProcessStepInstance projection not found for completion: {Id}.", @event.StepInstanceId);
                return;
            }

            await stepProjection.ReplaceInsertAsync(existing with
            {
                Status = StepInstanceStatus.Completed.Name,
                CompletedAt = @event.CompletedAt,
                UpdatedAt = @event.CompletedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step instance completion: {Id}.", @event.StepInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepInstanceCompleted)}: {ex.Message}", ex);
        }
    }
}