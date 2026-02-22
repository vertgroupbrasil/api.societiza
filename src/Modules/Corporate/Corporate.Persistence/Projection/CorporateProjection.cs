using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection;
using Corporate.Persistence.Context.Interfaces;

namespace Corporate.Persistence.Projection;

public class CorporateProjection<TProjection>(ICorporateProjectionDbContext context) : Projection<TProjection>(context), ICorporateProjection<TProjection>
    where TProjection : IProjectionModel
{
}