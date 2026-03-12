using Core.Persistence.Configurations;
using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace WorkflowTemplate.Persistence.Configurations;

public class SnapshotConfiguration
{
    public class WorkflowTemplateSnapshotConfiguration : SnapshotConfiguration<WorkflowTemplateAggregate>;
}