using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection;
using Identity.Persistence.Context.Interfaces;

namespace Identity.Persistence.Projection;

public class IdentityProjection<TProjection>(IIdentityProjectionDbContext context) : Projection<TProjection>(context), IIdentityProjection<TProjection>
    where TProjection : IProjectionModel
{
}