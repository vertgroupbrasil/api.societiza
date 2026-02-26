using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Accountancy.Shared.Queries;
using Accountancy.Shared.Response;
using Core.Application.Messaging.Queries;
using Core.Shared.Results;

namespace Accountancy.Application.UseCases.Queries;

public class GetAllAccountanciesHandler(
    IAccountancyProjection<ProjectionModel.Accountancy> projectionGateway) : IQueryHandler<GetAllAccountanciesQuery, List<AccountancyDetailResponse>>
{
    public async Task<Result<List<AccountancyDetailResponse>>> Handle(GetAllAccountanciesQuery request, CancellationToken cancellationToken)
    {
        var accountancies = await projectionGateway.ListAsync(x => !x.IsDeleted, cancellationToken);

        var response = accountancies
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
            .ToList();

        return Result.Success(response);
    }
}