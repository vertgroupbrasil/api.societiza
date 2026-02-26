using Accountancy.Shared.Response;
using Core.Application.Messaging.Queries;

namespace Accountancy.Shared.Queries;

public sealed record GetAccountancyDetailByIdQuery(Guid Id) : IQuery<AccountancyDetailResponse>;