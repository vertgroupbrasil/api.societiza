using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Domain.Events;
using WorkflowProcess.Domain.Projections;
using WorkflowProcess.Persistence.Projection;

namespace WorkflowProcess.Application.UseCases.Events.ProcessStepTask;

public interface IProjectionProcessStepTaskInstanceEventHandler :
    IEventHandler<DomainEvents.ProcessStepTaskInstanceMaterialized>,
    IEventHandler<DomainEvents.ProcessStepTaskInstanceCompleted>,
    IEventHandler<DomainEvents.ProcessStepTaskInstanceSkipped>,
    IEventHandler<DomainEvents.ProcessStepTaskInstanceReverted> { }

public class ProjectionProcessStepTaskInstanceEventHandler(
    IWorkflowProcessProjection<ProjectionModel.ProcessStepTaskInstance> taskProjection,
    ILogger<ProjectionProcessStepTaskInstanceEventHandler> logger) : IProjectionProcessStepTaskInstanceEventHandler
{
    public async Task Handle(DomainEvents.ProcessStepTaskInstanceReverted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await taskProjection.FindAsync(
                x => x.Id == @event.TaskInstanceId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("ProcessStepTaskInstance projection not found for revert: {Id}.", @event.TaskInstanceId);
                return;
            }

            await taskProjection.ReplaceInsertAsync(existing with
            {
                Status = TaskInstanceStatus.Pending.Name,
                CompletedAt = null,
                SkippedAt = null,
                UpdatedAt = @event.RevertedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step task instance revert: {Id}.", @event.TaskInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepTaskInstanceReverted)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.ProcessStepTaskInstanceMaterialized @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await taskProjection.ReplaceInsertAsync(new(
                @event.TaskInstanceId,
                @event.ProcessId,
                @event.StepInstanceId,
                @event.TemplateTaskId,
                @event.Title,
                @event.Order,
                @event.IsOptional,
                @event.Status,
                null,   // CompletedAt
                null,   // SkippedAt
                @event.CreatedAt,
                null),  // UpdatedAt
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step task instance materialization: {Id}.", @event.TaskInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepTaskInstanceMaterialized)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.ProcessStepTaskInstanceCompleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await taskProjection.FindAsync(
                x => x.Id == @event.TaskInstanceId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("ProcessStepTaskInstance projection not found for completion: {Id}.", @event.TaskInstanceId);
                return;
            }

            await taskProjection.ReplaceInsertAsync(existing with
            {
                Status = TaskInstanceStatus.Completed.Name,
                CompletedAt = @event.CompletedAt,
                UpdatedAt = @event.CompletedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step task instance completion: {Id}.", @event.TaskInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepTaskInstanceCompleted)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.ProcessStepTaskInstanceSkipped @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await taskProjection.FindAsync(
                x => x.Id == @event.TaskInstanceId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("ProcessStepTaskInstance projection not found for skip: {Id}.", @event.TaskInstanceId);
                return;
            }

            await taskProjection.ReplaceInsertAsync(existing with
            {
                Status = TaskInstanceStatus.Skipped.Name,
                SkippedAt = @event.SkippedAt,
                UpdatedAt = @event.SkippedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project step task instance skip: {Id}.", @event.TaskInstanceId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.ProcessStepTaskInstanceSkipped)}: {ex.Message}", ex);
        }
    }
}