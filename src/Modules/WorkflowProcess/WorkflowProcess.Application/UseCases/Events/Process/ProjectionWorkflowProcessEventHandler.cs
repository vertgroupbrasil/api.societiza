using Core.Application.EventBus;
using Microsoft.Extensions.Logging;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Domain.Events;
using WorkflowProcess.Domain.Projections;
using WorkflowProcess.Persistence.Projection;

namespace WorkflowProcess.Application.UseCases.Events.Process;

public interface IProjectionWorkflowProcessEventHandler :
    IEventHandler<DomainEvents.WorkflowProcessCreated>,
    IEventHandler<DomainEvents.WorkflowProcessCompleted>,
    IEventHandler<DomainEvents.WorkflowProcessReverted>,
    IEventHandler<DomainEvents.WorkflowProcessDeleted> { }

public class ProjectionWorkflowProcessEventHandler(
    IWorkflowProcessProjection<ProjectionModel.WorkflowProcess> workflowProcessProjection,
    ILogger<ProjectionWorkflowProcessEventHandler> logger) : IProjectionWorkflowProcessEventHandler
{
    public async Task Handle(DomainEvents.WorkflowProcessReverted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await workflowProcessProjection.FindAsync(
                x => x.Id == @event.ProcessId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("WorkflowProcess projection not found for revert: {Id}.", @event.ProcessId);
                return;
            }

            await workflowProcessProjection.ReplaceInsertAsync(existing with
            {
                Status = ProcessStatus.InProgress.Name,
                CompletedAt = null,
                UpdatedAt = @event.RevertedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project workflow process revert: {Id}.", @event.ProcessId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.WorkflowProcessReverted)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowProcessCreated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProcessProjection.ReplaceInsertAsync(new(
                @event.Id,
                @event.AccountancyId,
                @event.TemplateId,
                @event.TemplateVersion,
                @event.ProcessType,
                @event.TargetClient,
                @event.Status,
                @event.CreatedAt,
                null,   // CompletedAt
                null),  // UpdatedAt
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project workflow process creation: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.WorkflowProcessCreated)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowProcessDeleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowProcessProjection.UpdateOneFieldAsync(
                x => x.Id == @event.Id,
                x => x.IsDeleted,
                true,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project workflow process deletion: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.WorkflowProcessDeleted)}: {ex.Message}", ex);
        }
    }

    public async Task Handle(DomainEvents.WorkflowProcessCompleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await workflowProcessProjection.FindAsync(
                x => x.Id == @event.ProcessId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("WorkflowProcess projection not found for completion: {Id}.", @event.ProcessId);
                return;
            }

            await workflowProcessProjection.ReplaceInsertAsync(existing with
            {
                Status = ProcessStatus.Completed.Name,
                CompletedAt = @event.CompletedAt,
                UpdatedAt = @event.CompletedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project workflow process completion: {Id}.", @event.ProcessId);
            throw new InvalidOperationException($"Failed to project {nameof(DomainEvents.WorkflowProcessCompleted)}: {ex.Message}", ex);
        }
    }
}