using Core.Persistence.Projection.Abstractions;
using WorkflowProcess.Persistence.Context.Interfaces;

namespace WorkflowProcess.Persistence.Context;

public class WorkflowProcessProjectionDbContext(string connectionString)
    : MongoDbContext(connectionString), IWorkflowProcessProjectionDbContext
{
}