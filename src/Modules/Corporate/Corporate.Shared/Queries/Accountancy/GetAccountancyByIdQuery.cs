using Core.Application.Messaging.Queries;
using Corporate.Shared.Response.Accountancy;

namespace Corporate.Shared.Queries.Accountancy;

public sealed record GetAccountancyByIdQuery(Guid Id) : IQuery<AccountancyDetailResponse>;