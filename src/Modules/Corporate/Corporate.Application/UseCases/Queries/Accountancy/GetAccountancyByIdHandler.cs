using AutoMapper;
using Core.Application.Messaging.Queries;
using Core.Shared.Results;
using Corporate.Domain.Repositories;
using Corporate.Shared.Queries.Accountancy;
using Corporate.Shared.Response.Accountancy;

namespace Corporate.Application.UseCases.Queries.Accountancy;

public sealed class GetAccountancyByIdHandler : IQueryHandler<GetAccountancyByIdQuery, AccountancyDetailResponse>
{
    private readonly IMapper _mapper;
    private readonly IAccountancyRepository _accountancyRepository;

    public GetAccountancyByIdHandler(
        IMapper mapper,
        IAccountancyRepository accountancyRepository)
    {
        _mapper = mapper;
        _accountancyRepository = accountancyRepository;
    }

    public async Task<Result<AccountancyDetailResponse>> Handle(GetAccountancyByIdQuery request, CancellationToken cancellationToken)
    {
        var accountancy = await _accountancyRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);

        if (accountancy == null)
            return Result.Failure<AccountancyDetailResponse>(
                new Core.Shared.Errors.NotFoundError(
                    new Core.Shared.Errors.Error(
                        "Accountancy.NotFound",
                        $"Accountancy not found")));

        var response = _mapper.Map<AccountancyDetailResponse>(accountancy);
        return Result.Success(response);
    }
}