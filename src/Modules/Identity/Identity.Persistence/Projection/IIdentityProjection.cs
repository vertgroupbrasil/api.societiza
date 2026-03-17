using Core.Domain.Primitives.Interfaces;
using Core.Domain.Projection;

namespace Identity.Persistence.Projection;

public interface IIdentityProjection<TProjection> : IProjection<TProjection>
    where TProjection : IProjectionModel
{
}