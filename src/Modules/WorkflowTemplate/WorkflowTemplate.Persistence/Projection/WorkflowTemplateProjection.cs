using Core.Domain.Primitives.Interfaces;
using Core.Persistence.Projection;
using WorkflowTemplate.Persistence.Context.Interfaces;

namespace WorkflowTemplate.Persistence.Projection;

public class WorkflowTemplateProjection<TProjection>(IWorkflowTemplateProjectionDbContext context) : Projection<TProjection>(context), IWorkflowTemplateProjection<TProjection>
    where TProjection : IProjectionModel
{
}