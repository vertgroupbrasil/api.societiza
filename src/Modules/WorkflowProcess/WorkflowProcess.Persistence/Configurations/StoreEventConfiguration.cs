using Core.Persistence.Configurations;
using WorkflowProcessAggregate = WorkflowProcess.Domain.Aggregates.WorkflowProcess;

namespace WorkflowProcess.Persistence.Configurations;

public class StoreEventConfiguration
{
    public class WorkflowTemplateStoreEventConfiguration : StoreEventConfiguration<WorkflowProcessAggregate>;
}