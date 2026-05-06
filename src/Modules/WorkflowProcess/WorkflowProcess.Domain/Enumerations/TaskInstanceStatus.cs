using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace WorkflowProcess.Domain.Enumerations;

[method: JsonConstructor]
public class TaskInstanceStatus(string name, int value) : SmartEnum<TaskInstanceStatus>(name, value)
{
    public static readonly TaskInstanceStatus Pending = new PendingStatus();
    public static readonly TaskInstanceStatus Completed = new CompletedStatus();
    public static readonly TaskInstanceStatus Skipped = new SkippedStatus();

    public static implicit operator TaskInstanceStatus(string name)
        => FromName(name);

    public static implicit operator TaskInstanceStatus(int value)
        => FromValue(value);

    public static implicit operator string(TaskInstanceStatus status)
        => status.Name;

    public static implicit operator int(TaskInstanceStatus status)
        => status.Value;

    public class PendingStatus() : TaskInstanceStatus(nameof(Pending), 0) { }
    public class CompletedStatus() : TaskInstanceStatus(nameof(Completed), 1) { }
    public class SkippedStatus() : TaskInstanceStatus(nameof(Skipped), 2) { }
}