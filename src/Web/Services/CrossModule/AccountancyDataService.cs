using Accountancy.Shared.Queries;
using Identity.Application.Services;
using MediatR;

namespace Web.Services.CrossModule;

public sealed class AccountancyDataService(ISender sender) : IAccountancyDataService
{
    public async Task<bool> ExistsAsync(Guid accountancyId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountancyDetailByIdQuery(accountancyId), cancellationToken);
        return result.IsSuccess;
    }
}