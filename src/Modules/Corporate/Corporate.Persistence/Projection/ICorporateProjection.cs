using Core.Domain.Primitives.Interfaces;
using Core.Domain.Projection;

namespace Corporate.Persistence.Projection;

public interface ICorporateProjection<TProjection> : IProjection<TProjection>
    where TProjection : IProjectionModel
{
}