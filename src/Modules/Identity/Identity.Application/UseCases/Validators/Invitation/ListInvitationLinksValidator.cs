using Core.Application.Validators;
using Identity.Shared.Queries.Invitation;

namespace Identity.Application.UseCases.Validators.Invitation;

public sealed class ListInvitationLinksValidator : PagedQueryValidator<ListInvitationLinksQuery>
{
    public ListInvitationLinksValidator()
    {
        AddPaginationRules(
            pageNumberSelector: x => x.PageNumber,
            pageSizeSelector: x => x.PageSize);
    }
}