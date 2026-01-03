using AutoMapper;
using Core.Application.Messaging.Commands;
using Core.Domain.Entities;
using Core.Persistence.Context;
using Core.Persistence.UnitOfWork;
using Core.Shared.Extensions;
using Core.Shared.Results;
using Corporate.Domain.Repositories;
using Corporate.Shared.Commands.Accountancy;
using Corporate.Shared.Response.Accountancy;

namespace Corporate.Application.UseCases.Commands.Accountancy;

public sealed class CreateAccountancyHandler : ICommandHandler<CreateAccountancyCommand, CreateAccountancyResponse>
{
    private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
    private readonly IMapper _mapper;

    private readonly IAccountancyRepository _accountancyRepository;
    private readonly ITenantRepository _tenantRepository;

    public CreateAccountancyHandler(
        IUnitOfWork<ApplicationDbContext> unitOfWork,
        IMapper mapper,

        IAccountancyRepository accountancyRepository,
        ITenantRepository tenantRepository)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;

        _accountancyRepository = accountancyRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<CreateAccountancyResponse>> Handle(CreateAccountancyCommand request, CancellationToken cancellationToken)
    {
        if (await _accountancyRepository.ExistsAsync(a => a.CNPJ == request.CNPJ.OnlyDigits(), cancellationToken))
            return Result.Failure<CreateAccountancyResponse>(
                new Core.Shared.Errors.Error(
                    "Accountancy.AlreadyExists",
                    "An accountancy with this CNPJ already exists.")
            );

        var tenant = Tenant.Create(
            name: request.LegalName,
            cnpj: request.CNPJ,
            databaseProvider: request.DatabaseProvider,
            connectionString: request.DatabaseConnectionString,
            expirationDate: request.ExpirationDate
        );

        var accountancy = Domain.Entities.Accountancy.Create(
            tenantId: tenant.Id,
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

        await _unitOfWork.ExecuteAsync(async ct => 
        {
            _tenantRepository.Add(tenant);
            _accountancyRepository.Add(accountancy);
        }, cancellationToken);

        var response = _mapper.Map<CreateAccountancyResponse>(accountancy);
        return Result.Success(response);
    }
}