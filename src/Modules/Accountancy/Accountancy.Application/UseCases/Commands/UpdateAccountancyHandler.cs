using Accountancy.Application.Services;
using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Accountancy.Shared.Commands;
using Core.Application.Messaging.Commands;
using Core.Shared.Extensions;
using Core.Shared.Results;

namespace Accountancy.Application.UseCases.Commands;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

public class UpdateAccountancyHandler(
    IAccountancyApplicationService applicationService,
    IAccountancyProjection<ProjectionModel.Accountancy> accountancyProjection) : ICommandHandler<UpdateAccountancyCommand>
{
    public async Task<Result> Handle(UpdateAccountancyCommand request, CancellationToken cancellationToken)
    {
        var normalizedCnpj = request.CNPJ.OnlyDigits();

        var conflict = await accountancyProjection.FindAsync(
            x => x.CNPJ == normalizedCnpj && x.Id != request.Id,
            cancellationToken);

        if (conflict is not null)
            return Result.Failure(new Core.Shared.Errors.ConflictError(new(
                "CNPJAlreadyExists", "An accountancy with the same CNPJ already exists")));

        var accountancyResult = await applicationService.LoadAggregateAsync<AccountancyAggregate>(request.Id, cancellationToken);

        if (!accountancyResult.IsSuccess)
            return Result.Failure(accountancyResult.Error);

        var accountancy = accountancyResult.Value;

        accountancy.Update(
            cnpj: request.CNPJ,
            legalName: request.LegalName,
            tradeName: request.TradeName,
            address: request.Address,
            city: request.City,
            state: request.State,
            postalCode: request.PostalCode,
            phone: request.Phone,
            email: request.Email
        );

        await applicationService.AppendEventsAsync(accountancy, cancellationToken);
        return Result.Success();
    }
}