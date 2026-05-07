using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Shared.Results;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Shared.Commands.Process;
using WorkflowProcess.Shared.Errors;

namespace WorkflowProcess.Application.UseCases.Commands.Process;

public sealed class DeleteWorkflowProcessHandler(
    IWorkflowProcessApplicationService applicationService,
    ICurrentUserService currentUser
) : ICommandHandler<DeleteWorkflowProcessCommand>
{
    public async Task<Result> Handle(DeleteWorkflowProcessCommand request, CancellationToken cancellationToken)
    {
        var processResult = await applicationService.LoadAggregateAsync<Domain.Aggregates.WorkflowProcess>(request.Id, cancellationToken);

        if (!processResult.IsSuccess)
            return Result.Failure(WorkflowProcessErrors.ProcessNotFound);

        var process = processResult.Value;

        if (currentUser.Role != IdentityRoles.SystemAdmin && currentUser.AccountancyId != process.AccountancyId)
            return Result.Failure(WorkflowProcessErrors.Forbidden);

        process.Delete();

        await applicationService.AppendEventsAsync(process, cancellationToken);
        return Result.Success();
    }
}
