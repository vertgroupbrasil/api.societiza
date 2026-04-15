using Core.Domain.Primitives.Interfaces;
using Core.Domain.Projection;

namespace WorkflowProcess.Persistence.Projection;

public interface IWorkflowProcessProjection<TProjection> : IProjection<TProjection>
    where TProjection : IProjectionModel
{
}
