using Core.Domain.Primitives.Interfaces;
using Core.Domain.Projection;

namespace Accountancy.Persistence.Projection;

public interface IAccountancyProjection<TProjection> : IProjection<TProjection>
    where TProjection : IProjectionModel
{
}