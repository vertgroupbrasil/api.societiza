using Accountancy.Shared.Queries;
using Core.Application.Validators;

namespace Accountancy.Application.UseCases.Validators;

public sealed class GetAllAccountanciesValidator : PagedQueryValidator<GetAllAccountanciesQuery>
{
    public GetAllAccountanciesValidator()
    {
        AddPaginationRules(
            pageNumberSelector: x => x.PageNumber,
            pageSizeSelector: x => x.PageSize);
    }
}