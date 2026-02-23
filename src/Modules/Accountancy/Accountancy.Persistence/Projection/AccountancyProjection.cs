using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection;
using Accountancy.Persistence.Context.Interfaces;

namespace Accountancy.Persistence.Projection;

public class AccountancyProjection<TProjection>(IAccountancyProjectionDbContext context) : Projection<TProjection>(context), IAccountancyProjection<TProjection>
    where TProjection : IProjectionModel
{
}