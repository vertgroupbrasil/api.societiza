using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection;
using WorkflowProcess.Persistence.Context.Interfaces;

namespace WorkflowProcess.Persistence.Projection;

public class WorkflowProcessProjection<TProjection>(IWorkflowProcessProjectionDbContext context)
    : Projection<TProjection>(context), IWorkflowProcessProjection<TProjection>
    where TProjection : IProjectionModel
{
}
