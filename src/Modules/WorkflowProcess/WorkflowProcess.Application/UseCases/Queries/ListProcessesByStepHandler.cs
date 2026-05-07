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
            x => x.AccountancyId == accountancyId
                 && !x.IsDeleted
                 && (request.TemplateId == null || x.TemplateId == request.TemplateId),
            cancellationToken);

        var processIds = processes.Select(p => p.Id).ToList();
        var processById = processes.ToDictionary(p => p.Id);

        var allSteps = processIds.Any()
            ? await stepProjection.ListAsync(x => processIds.Contains(x.ProcessId), cancellationToken)
            : [];

        var allUniqueSteps = allSteps
            .GroupBy(s => new { s.Title, s.Order })
            .Select(g => new { g.Key.Title, g.Key.Order })
            .OrderBy(s => s.Order)
            .ToList();

        if (!allUniqueSteps.Any())
            return Result.Success(PagedResult<ProcessStepGroupResponse>.Create(
                paging,
                Enumerable.Empty<ProcessStepGroupResponse>().AsQueryable()));

        var currentStepByProcess = allSteps
            .GroupBy(s => s.ProcessId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var inProgress = g.FirstOrDefault(s => s.Status == "InProgress");
                    if (inProgress is not null) return inProgress;

                    var firstNotStarted = g.OrderBy(s => s.Order).FirstOrDefault(s => s.Status == "NotStarted");
                    if (firstNotStarted is not null) return firstNotStarted;

                    return g.OrderByDescending(s => s.Order).First();
                });

        var stepGroups = allUniqueSteps
            .Select(step =>
            {
                var processItems = currentStepByProcess.Values
                    .Where(s => s.Title == step.Title && s.Order == step.Order)
                    .OrderByDescending(s => processById.TryGetValue(s.ProcessId, out var p) ? p.CreatedAt : default)
                    .Select(s =>
                    {
                        var proc = processById[s.ProcessId];
                        return new ProcessByStepItemResponse(
                            ProcessId:      proc.Id,
                            StepInstanceId: s.Id,
                            ProcessType:    proc.ProcessType,
                            TargetClient:   proc.TargetClient,
                            ProcessStatus:  proc.Status,
                            StepStatus:     s.Status,
                            CreatedAt:      proc.CreatedAt,
                            CompletedAt:    proc.CompletedAt);
                    })
                    .ToList();

                return new ProcessStepGroupResponse(
                    StepTitle:    step.Title,
                    StepOrder:    step.Order,
                    ProcessCount: processItems.Count,
                    Processes:    processItems);
            })
            .AsQueryable();

        return Result.Success(PagedResult<ProcessStepGroupResponse>.Create(paging, stepGroups));
    }
}