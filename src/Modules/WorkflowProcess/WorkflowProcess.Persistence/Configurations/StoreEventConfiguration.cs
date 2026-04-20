using Core.Persistence.Configurations;

namespace WorkflowProcess.Persistence.Configurations;

using WorkflowProcessAggregate = WorkflowProcess.Domain.Aggregates.WorkflowProcess;

public class StoreEventConfiguration
{
    public class WorkflowProcessStoreEventConfiguration : StoreEventConfiguration<WorkflowProcessAggregate>;
}