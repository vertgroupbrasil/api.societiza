using Core.Persistence.Projection.Abstractions;
using Workflow.Persistence.Context.Interfaces;

namespace Workflow.Persistence.Context;

public class WorkflowProjectionDbContext(string connectionString) : MongoDbContext(connectionString), IWorkflowProjectionDbContext
{
}