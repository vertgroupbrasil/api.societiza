using Core.Application.Messaging.Queries;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Shared.Results;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Domain.Projections;
using WorkflowProcess.Persistence.Projection;
using WorkflowProcess.Shared.Errors;
using WorkflowProcess.Shared.Queries;
using WorkflowProcess.Shared.Response.Process;

namespace WorkflowProcess.Application.UseCases.Queries;

public sealed class GetProcessBoardHandler(
    IWorkflowProcessProjection<ProjectionModel.WorkflowProcess> processProjection,
    IWorkflowProcessProjection<ProjectionModel.ProcessStepInstance> stepProjection,
    ICurrentUserService currentUser
) : IQueryHandler<GetProcessBoardQuery, List<ProcessBoardItemResponse>>
{
    public async Task<Result<List<ProcessBoardItemResponse>>> Handle(
        GetProcessBoardQuery request, CancellationToken cancellationToken)
    {
        var accountancyId = currentUser.Role == IdentityRoles.SystemAdmin
            ? request.AccountancyId
            : currentUser.AccountancyId ?? request.AccountancyId;

        if (currentUser.Role != IdentityRoles.SystemAdmin && currentUser.AccountancyId != request.AccountancyId)
            return Result.Failure<List<ProcessBoardItemResponse>>(WorkflowProcessErrors.Forbidden);

        var processes = await processProjection.ListAsync(
            x => x.AccountancyId == accountancyId
              && !x.IsDeleted
              && (request.ProcessType == null || x.ProcessType == request.ProcessType),
            cancellationToken);

        if (!processes.Any())
            return Result.Success(new List<ProcessBoardItemResponse>());

        var processIds = processes.Select(p => p.Id).ToList();
        var allSteps = await stepProjection.ListAsync(
            x => processIds.Contains(x.ProcessId),
            cancellationToken);

        var response = processes
            .OrderByDescending(p => p.CreatedAt)
            .Select(p =>
            {
                var steps = allSteps.Where(s => s.ProcessId == p.Id).OrderBy(s => s.Order).ToList();
                var currentStep = steps.FirstOrDefault(s => s.Status != StepInstanceStatus.Completed.Name);

                return new ProcessBoardItemResponse(
                    p.Id,
                    p.ProcessType,
                    p.TargetClient,
                    p.Status,
                    currentStep?.Title,
                    steps.Count,
                    steps.Count(s => s.Status == StepInstanceStatus.Completed.Name),
                    p.CreatedAt,
                    p.CompletedAt);
            })
            .ToList();

        return Result.Success(response);
    }
}
