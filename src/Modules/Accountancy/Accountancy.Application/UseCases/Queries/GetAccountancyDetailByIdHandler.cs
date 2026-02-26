using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Accountancy.Shared.Queries;
using Accountancy.Shared.Response;
using Core.Application.Messaging.Queries;
using Core.Shared.Errors;
using Core.Shared.Results;

namespace Accountancy.Application.UseCases.Queries;

public class GetAccountancyDetailByIdHandler(
    IAccountancyProjection<ProjectionModel.Accountancy> projectionGateway) : IQueryHandler<GetAccountancyDetailByIdQuery, AccountancyDetailResponse>
{
    public async Task<Result<AccountancyDetailResponse>> Handle(GetAccountancyDetailByIdQuery request, CancellationToken cancellationToken)
    {
        var accountancy = await projectionGateway.GetByIdAsync(request.Id, cancellationToken);

        if (accountancy == null)
            return Result.Failure<AccountancyDetailResponse>(new NotFoundError(new(
                "AccountancyNotFound", $"Accountancy {request.Id} not found")));

        return Result.Success(new AccountancyDetailResponse(
            accountancy.Id,
            accountancy.CNPJ,
            accountancy.LegalName,
            accountancy.TradeName,
            accountancy.Address,
            accountancy.City,
            accountancy.State,
            accountancy.PostalCode,
            accountancy.Phone,
            accountancy.Email,
            accountancy.CreatedAt,
            accountancy.UpdatedAt));
    }
}