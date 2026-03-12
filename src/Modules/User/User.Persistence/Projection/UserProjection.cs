using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection;
using User.Persistence.Context.Interfaces;

namespace User.Persistence.Projection;

public class UserProjection<TProjection>(IUserProjectionDbContext context) : Projection<TProjection>(context), IUserProjection<TProjection>
    where TProjection : IProjectionModel
{
}