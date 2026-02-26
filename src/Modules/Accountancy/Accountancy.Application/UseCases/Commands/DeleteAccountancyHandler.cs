using Accountancy.Application.Services;
using Accountancy.Shared.Commands;
using Core.Application.Messaging.Commands;
using Core.Shared.Results;

namespace Accountancy.Application.UseCases.Commands;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

public class DeleteAccountancyHandler(
    IAccountancyApplicationService applicationService) : ICommandHandler<DeleteAccountancyCommand>
{
    public async Task<Result> Handle(DeleteAccountancyCommand request, CancellationToken cancellationToken)
    {
        var accountancyResult = await applicationService.LoadAggregateAsync<AccountancyAggregate>(request.Id, cancellationToken);

        if (!accountancyResult.IsSuccess)
            return Result.Failure(accountancyResult.Error);

        var accountancy = accountancyResult.Value;
        
        accountancy.Delete();

        await applicationService.AppendEventsAsync(accountancy, cancellationToken);
        return Result.Success();
    }
}