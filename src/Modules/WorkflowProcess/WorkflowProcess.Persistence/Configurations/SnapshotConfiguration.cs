using Core.Persistence.Configurations;
using WorkflowProcessAggregate = WorkflowProcess.Domain.Aggregates.WorkflowProcess;

namespace WorkflowProcess.Persistence.Configurations;

public class SnapshotConfiguration
{
    public class WorkflowProcessSnapshotConfiguration : SnapshotConfiguration<WorkflowProcessAggregate>;
}
