using Core.Domain.Primitives.Interfaces;
using Core.Domain.Projection;

namespace WorkflowTemplate.Persistence.Projection;

public interface IWorkflowTemplateProjection<TProjection> : IProjection<TProjection>
    where TProjection : IProjectionModel
{
}