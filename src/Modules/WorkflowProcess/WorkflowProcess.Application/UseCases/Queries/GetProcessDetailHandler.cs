using Core.Application.Messaging.Queries;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Shared.Errors;
using Core.Shared.Results;
using WorkflowProcess.Domain.Projections;
using WorkflowProcess.Persistence.Projection;
using WorkflowProcess.Shared.Errors;
using WorkflowProcess.Shared.Queries;
using WorkflowProcess.Shared.Response.Process;
using WorkflowProcess.Shared.Response.ProcessStep;
using WorkflowProcess.Shared.Response.ProcessStepTask;

namespace WorkflowProcess.Application.UseCases.Queries;

public sealed class GetProcessDetailHandler(
    IWorkflowProcessProjection<ProjectionModel.WorkflowProcess> processProjection,
    IWorkflowProcessProjection<ProjectionModel.ProcessStepInstance> stepProjection,
    IWorkflowProcessProjection<ProjectionModel.ProcessStepTaskInstance> taskProjection,
    IWorkflowProcessProjection<ProjectionModel.ProcessStepFieldInstance> stepFieldProjection,
    IWorkflowProcessProjection<ProjectionModel.ProcessStepTaskFieldInstance> taskFieldProjection,
    ICurrentUserService currentUser
) : IQueryHandler<GetProcessDetailQuery, ProcessDetailResponse>
{
    public async Task<Result<ProcessDetailResponse>> Handle(
        GetProcessDetailQuery request, CancellationToken cancellationToken)
    {
        var process = await processProjection.GetByIdAsync(request.ProcessId, cancellationToken);

        if (process is null)
            return Result.Failure<ProcessDetailResponse>(new NotFoundError(new(
                "WorkflowProcess.NotFound", "Workflow process not found.")));

        if (currentUser.Role != IdentityRoles.SystemAdmin && currentUser.AccountancyId != process.AccountancyId)
            return Result.Failure<ProcessDetailResponse>(WorkflowProcessErrors.Forbidden);

        var stepsTask      = stepProjection.ListAsync(x => x.ProcessId == request.ProcessId, cancellationToken);
        var tasksTask      = taskProjection.ListAsync(x => x.ProcessId == request.ProcessId, cancellationToken);
        var stepFieldsTask = stepFieldProjection.ListAsync(x => x.ProcessId == request.ProcessId, cancellationToken);
        var taskFieldsTask = taskFieldProjection.ListAsync(x => x.ProcessId == request.ProcessId, cancellationToken);

        await Task.WhenAll(stepsTask, tasksTask, stepFieldsTask, taskFieldsTask);

        var steps      = await stepsTask;
        var tasks      = await tasksTask;
        var stepFields = await stepFieldsTask;
        var taskFields = await taskFieldsTask;

        var stepResponses = steps
            .OrderBy(s => s.Order)
            .Select(s => new ProcessStepInstanceResponse(
                s.Id,
                s.Title,
                s.Order,
                s.Status,
                s.CompletedAt,
                tasks.Where(t => t.StepInstanceId == s.Id)
                    .OrderBy(t => t.Order)
                    .Select(t => new ProcessStepTaskInstanceResponse(
                        t.Id,
                        t.Title,
                        t.Order,
                        t.IsOptional,
                        t.Status,
                        t.CompletedAt,
                        t.SkippedAt,
                        taskFields.Where(f => f.TaskInstanceId == t.Id)
                            .OrderBy(f => f.Order)
                            .Select(f => new ProcessStepTaskFieldInstanceResponse(
                                f.Id, f.Label, f.FieldType, f.Options, f.Order, f.Value, f.FilledAt))
                            .ToList()))
                    .ToList(),
                stepFields.Where(f => f.StepInstanceId == s.Id)
                    .OrderBy(f => f.Order)
                    .Select(f => new ProcessStepFieldInstanceResponse(
                        f.Id, f.Label, f.FieldType, f.Options, f.Order, f.Value, f.FilledAt))
                    .ToList()))
            .ToList();

        return Result.Success(new ProcessDetailResponse(
            process.Id,
            process.AccountancyId,
            process.TemplateId,
            process.TemplateVersion,
            process.ProcessType,
            process.TargetClient,
            process.Status,
            process.CreatedAt,
            process.CompletedAt,
            stepResponses));
    }
}
