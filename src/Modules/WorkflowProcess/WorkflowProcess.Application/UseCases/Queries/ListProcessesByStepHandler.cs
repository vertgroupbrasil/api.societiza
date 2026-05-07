using Core.Application.Messaging.Queries;
using Core.Application.Pagination;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Domain.Pagination;
using Core.Domain.Pagination.Interfaces;
using Core.Shared.Results;
using WorkflowProcess.Domain.Projections;
using WorkflowProcess.Persistence.Projection;
using WorkflowProcess.Shared.Errors;
using WorkflowProcess.Shared.Queries;
using WorkflowProcess.Shared.Response.Process;

namespace WorkflowProcess.Application.UseCases.Queries;

public sealed class ListProcessesByStepHandler(
    IWorkflowProcessProjection<ProjectionModel.WorkflowProcess> processProjection,
    IWorkflowProcessProjection<ProjectionModel.ProcessStepInstance> stepProjection,
    ICurrentUserService currentUser
) : IQueryHandler<ListProcessesByStepQuery, IPagedResult<ProcessStepGroupResponse>>
{
    public async Task<Result<IPagedResult<ProcessStepGroupResponse>>> Handle(
        ListProcessesByStepQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != IdentityRoles.SystemAdmin && currentUser.AccountancyId != request.AccountancyId)
            return Result.Failure<IPagedResult<ProcessStepGroupResponse>>(WorkflowProcessErrors.Forbidden);

        var accountancyId = currentUser.Role == IdentityRoles.SystemAdmin
            ? request.AccountancyId
            : currentUser.AccountancyId ?? request.AccountancyId;

        var paging = new Paging(request.PageSize, request.PageNumber);

        var processes = await processProjection.ListAsync(
            x => x.AccountancyId == accountancyId && !x.IsDeleted,
            cancellationToken);

        if (!processes.Any())
            return Result.Success(PagedResult<ProcessStepGroupResponse>.Create(
                paging,
                Enumerable.Empty<ProcessStepGroupResponse>().AsQueryable()));

        var processIds = processes.Select(p => p.Id).ToList();
        var processById = processes.ToDictionary(p => p.Id);

        var allSteps = await stepProjection.ListAsync(
            x => processIds.Contains(x.ProcessId),
            cancellationToken);

        var currentSteps = allSteps
            .GroupBy(s => s.ProcessId)
            .Select(g => g.Where(s => s.Status != "Completed").OrderBy(s => s.Order).FirstOrDefault())
            .Where(s => s is not null)
            .Select(s => s!);

        var stepGroups = currentSteps
            .GroupBy(s => new { s.Title, s.Order })
            .Select(g =>
            {
                var processItems = g
                    .OrderByDescending(s => processById.TryGetValue(s.ProcessId, out var p) ? p.CreatedAt : default)
                    .Select(s =>
                    {
                        var proc = processById[s.ProcessId];
                        return new ProcessByStepItemResponse(
                            ProcessId:     proc.Id,
                            StepInstanceId: s.Id,
                            ProcessType:   proc.ProcessType,
                            TargetClient:  proc.TargetClient,
                            ProcessStatus: proc.Status,
                            StepStatus:    s.Status,
                            CreatedAt:     proc.CreatedAt,
                            CompletedAt:   proc.CompletedAt);
                    })
                    .ToList();

                return new ProcessStepGroupResponse(
                    StepTitle:    g.Key.Title,
                    StepOrder:    g.Key.Order,
                    ProcessCount: processItems.Count,
                    Processes:    processItems);
            })
            .OrderBy(g => g.StepOrder)
            .AsQueryable();

        return Result.Success(PagedResult<ProcessStepGroupResponse>.Create(paging, stepGroups));
    }
}
