using Core.Persistence.Configurations;
using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Persistence.Configurations;

public class StoreEventConfiguration
{
    public class WorkflowTemplateStoreEventConfiguration : StoreEventConfiguration<WorkflowTemplateAggregate>;
}