using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Accountancy.Shared.Queries;
using Accountancy.Shared.Response;
using Core.Application.Messaging.Queries;
using Core.Application.Pagination;
using Core.Domain.Pagination;
using Core.Domain.Pagination.Interfaces;
using Core.Shared.Results;

namespace Accountancy.Application.UseCases.Queries;

public class GetAllAccountanciesHandler(
    IAccountancyProjection<ProjectionModel.Accountancy> projectionGateway) : IQueryHandler<GetAllAccountanciesQuery, IPagedResult<AccountancyDetailResponse>>
{
    public async Task<Result<IPagedResult<AccountancyDetailResponse>>> Handle(GetAllAccountanciesQuery request, CancellationToken cancellationToken)
    {
        var accountancies = await projectionGateway.ListAsync(x => !x.IsDeleted, cancellationToken);

        var ordered = accountancies
            .OrderBy(a => a.LegalName)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new AccountancyDetailResponse(
                a.Id,
                a.CNPJ,
                a.LegalName,
                a.TradeName,
                a.Address,
                a.City,
                a.State,
                a.PostalCode,
                a.Phone,
                a.Email,
                a.CreatedAt,
                a.UpdatedAt))
            .AsQueryable();

        var paging = new Paging(request.PageSize, request.PageNumber);
        return Result.Success(PagedResult<AccountancyDetailResponse>.Create(paging, ordered));
    }
}