using Accountancy.Application.Services;
using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Accountancy.Shared.Commands;
using Core.Application.Messaging.Commands;
using Core.Shared.Extensions;
using Core.Shared.Response;
using Core.Shared.Results;

namespace Accountancy.Application.UseCases.Commands;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

public class CreateAccountancyHandler(
    IAccountancyApplicationService applicationService,
    IAccountancyProjection<ProjectionModel.Accountancy> accountancyProjection) : ICommandHandler<CreateAccountancyCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(CreateAccountancyCommand request, CancellationToken cancellationToken)
    {
        var existingAccountancy = await accountancyProjection.FindAsync(x => x.CNPJ == request.CNPJ.OnlyDigits(), cancellationToken);

        if (existingAccountancy is not null)
            return Result.Failure<IdentifierResponse>(new Core.Shared.Errors.ConflictError(new(
                "CNPJAlreadyExists", "An accountancy with the same CNPJ already exists")));

        var accountancy = AccountancyAggregate.Create(
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
        return Result.Success(new IdentifierResponse(accountancy.Id));
    }
}