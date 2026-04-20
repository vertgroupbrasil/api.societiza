using Core.Persistence.Configurations;

namespace WorkflowProcess.Persistence.Configurations;

using WorkflowProcessAggregate = WorkflowProcess.Domain.Aggregates.WorkflowProcess;

public class SnapshotConfiguration
{
    public class WorkflowProcessSnapshotConfiguration : SnapshotConfiguration<WorkflowProcessAggregate>;
}