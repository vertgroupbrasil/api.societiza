using Core.Application.Messaging.Commands;
using Core.Persistence.Context;
using Core.Persistence.UnitOfWork;
using Core.Shared.Results;
using Corporate.Domain.Repositories;
using Corporate.Shared.Commands.Accountancy;

namespace Corporate.Application.UseCases.Commands.Accountancy;

public sealed class UpdateAccountancyHandler : ICommandHandler<UpdateAccountancyCommand>
{
    private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
    private readonly IAccountancyRepository _accountancyRepository;
    private readonly ITenantRepository _tenantRepository;

    public UpdateAccountancyHandler(
        IUnitOfWork<ApplicationDbContext> unitOfWork,
        IAccountancyRepository accountancyRepository,
        ITenantRepository tenantRepository)
    {
        _unitOfWork = unitOfWork;
        _accountancyRepository = accountancyRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<Result> Handle(UpdateAccountancyCommand request, CancellationToken cancellationToken)
    {
        var accountancy = await _accountancyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (accountancy is null)
            return Result.Failure(
                new Core.Shared.Errors.NotFoundError(
                    new Core.Shared.Errors.Error(
                        "Accountancy.NotFound",
                        "Accountancy not found")));

        var tenant = await _tenantRepository.GetByIdAsync(accountancy.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Failure(
                new Core.Shared.Errors.NotFoundError(
                    new Core.Shared.Errors.Error(
                        "Tenant.NotFound",
                        "Tenant not found")));

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            accountancy.Update(
                legalName: request.LegalName,
                tradeName: request.TradeName ?? string.Empty,
                cnpj: request.CNPJ,
                address: request.Address,
                city: request.City,
                state: request.State,
                postalCode: request.PostalCode,
                phone: request.Phone,
                email: request.Email,
                notes: request.Notes
            );

            tenant.Update(
                name: request.LegalName,
                cnpj: request.CNPJ,
                databaseProvider: request.DatabaseProvider,
                connectionString: request.DatabaseConnectionString
            );

            _accountancyRepository.Update(accountancy);
            _tenantRepository.Update(tenant);
        }, cancellationToken);

        return Result.Success();
    }
}