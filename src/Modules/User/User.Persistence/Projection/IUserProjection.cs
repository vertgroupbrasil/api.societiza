using Core.Domain.Primitives.Interfaces;
using Core.Domain.Projection;

namespace User.Persistence.Projection;

public interface IUserProjection<TProjection> : IProjection<TProjection>
    where TProjection : IProjectionModel
{
}