using Core.Persistence.Configurations;

namespace WorkflowTemplate.Persistence.Configurations;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

public class SnapshotConfiguration
{
    public class WorkflowTemplateSnapshotConfiguration : SnapshotConfiguration<WorkflowTemplateAggregate>;
}