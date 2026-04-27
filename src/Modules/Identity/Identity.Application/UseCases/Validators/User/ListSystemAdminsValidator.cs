using Core.Application.Validators;
using Identity.Shared.Queries.User;

namespace Identity.Application.UseCases.Validators.User;

public sealed class ListSystemAdminsValidator : PagedQueryValidator<ListSystemAdminsQuery>
{
    public ListSystemAdminsValidator()
    {
        AddPaginationRules(
            pageNumberSelector: x => x.PageNumber,
            pageSizeSelector: x => x.PageSize);
    }
}
