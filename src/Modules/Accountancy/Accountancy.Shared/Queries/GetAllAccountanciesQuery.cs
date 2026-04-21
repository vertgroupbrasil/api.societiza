using Accountancy.Shared.Response;
using Core.Application.Messaging.Queries;
using Core.Domain.Pagination.Interfaces;

namespace Accountancy.Shared.Queries;

public sealed record GetAllAccountanciesQuery(int PageNumber, int PageSize) : IQuery<IPagedResult<AccountancyDetailResponse>>;