using Core.Persistence.Projection.Abstractions;
using WorkflowTemplate.Persistence.Context.Interfaces;

namespace WorkflowTemplate.Persistence.Context;

public class WorkflowTemplateProjectionDbContext(string connectionString) : MongoDbContext(connectionString), IWorkflowTemplateProjectionDbContext
{
}