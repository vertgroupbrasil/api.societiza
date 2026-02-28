using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection;
using Workflow.Persistence.Context.Interfaces;

namespace Workflow.Persistence.Projection;

public class WorkflowProjection<TProjection>(IWorkflowProjectionDbContext context) : Projection<TProjection>(context), IWorkflowProjection<TProjection>
    where TProjection : IProjectionModel
{
}