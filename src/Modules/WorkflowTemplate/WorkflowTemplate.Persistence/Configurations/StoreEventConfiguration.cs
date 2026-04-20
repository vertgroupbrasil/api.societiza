using Core.Persistence.Configurations;

namespace WorkflowTemplate.Persistence.Configurations;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class StoreEventConfiguration
{
    public class WorkflowTemplateStoreEventConfiguration : StoreEventConfiguration<WorkflowTemplateAggregate>;
}