using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Application.Messaging.Queries;
using Core.Application.Models;
using Core.Shared.Extensions;
using Core.Shared.Results;
using Corporate.Domain.Repositories;
using Corporate.Shared.Queries.Accountancy;
using Corporate.Shared.Response.Accountancy;
using Microsoft.EntityFrameworkCore;

namespace Corporate.Application.UseCases.Queries.Accountancy;

public sealed class GetAccountanciesHandler : IQueryHandler<GetAccountanciesQuery, PagedResult<AccountancyListResponse>>
{
    private readonly IMapper _mapper;
    private readonly IAccountancyRepository _accountancyRepository;

    public GetAccountanciesHandler(
        IMapper mapper,
        IAccountancyRepository accountancyRepository)
    {
        _mapper = mapper;
        _accountancyRepository = accountancyRepository;
    }

    public async Task<Result<PagedResult<AccountancyListResponse>>> Handle(GetAccountanciesQuery request, CancellationToken cancellationToken)
    {
        var query = _accountancyRepository.GetQueryable();

        query = query.Include(a => a.Tenant)
                     .IgnoreQueryFilters();

        if (!request.IncludeDeleted)
        {
            query = query.Where(a => !a.IsDeleted && !a.Tenant.IsDeleted);
        }

        if (!request.IncludeExpired)
        {
            query = query.Where(a => a.Tenant.ExpirationDate > DateTimeOffset.UtcNow);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchNormalized = request.SearchTerm.Trim().ToLowerInvariant();
            var onlyDigits = request.SearchTerm.OnlyDigits();

            query = query.Where(a =>
                a.LegalName.ToLower().Contains(searchNormalized) ||
                (a.TradeName != null && a.TradeName.ToLower().Contains(searchNormalized)) ||
                (onlyDigits.Length > 0 && a.CNPJ.Contains(onlyDigits)) ||
                a.Email.ToLower().Contains(searchNormalized) ||
                a.Tenant.Name.ToLower().Contains(searchNormalized));
        }

        if (!string.IsNullOrWhiteSpace(request.State))
        {
            var stateNormalized = request.State.Trim().ToUpperInvariant();
            query = query.Where(a => a.State == stateNormalized);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var cityNormalized = request.City.Trim().ToLowerInvariant();
            query = query.Where(a => a.City.ToLower() == cityNormalized);
        }

        if (request.HasDedicatedDatabase.HasValue)
        {
            if(request.HasDedicatedDatabase.Value)
            {
                query = query.Where(a =>
                    a.Tenant.ConnectionString != null &&
                    a.Tenant.ConnectionString != "");
            }
            else
            {
                query = query.Where(a =>
                    a.Tenant.ConnectionString == null ||
                    a.Tenant.ConnectionString == "");
            }
        }

        query = query.OrderByDescending(a => a.CreatedAt)
                     .ThenBy(a => a.LegalName);

        var projectedQuery = query.ProjectTo<AccountancyListResponse>(_mapper.ConfigurationProvider);

        var pagedResult = await PagedResult<AccountancyListResponse>.CreateAsync(
            projectedQuery,
            request.PageNumber,
            request.PageSize);

        return Result.Success(pagedResult);
    }
}