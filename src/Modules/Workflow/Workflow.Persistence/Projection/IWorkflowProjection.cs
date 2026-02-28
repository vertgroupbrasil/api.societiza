using Core.Domain.Primitives.Interfaces;
using Core.Domain.Projection;

namespace Workflow.Persistence.Projection;

public interface IWorkflowProjection<TProjection> : IProjection<TProjection>
    where TProjection : IProjectionModel
{
}