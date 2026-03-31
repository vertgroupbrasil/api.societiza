using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace WorkflowTemplate.Domain.Enumerations;

[method: JsonConstructor]
public class TaskType(string name, int value) : SmartEnum<TaskType>(name, value)
{
    public static readonly TaskType Manual = new ManualType();
    public static readonly TaskType Automatic = new AutomaticType();
    public static readonly TaskType Approval = new ApprovalType();

    public static implicit operator TaskType(string name)
        => FromName(name);

    public static implicit operator TaskType(int value)
        => FromValue(value);

    public static implicit operator string(TaskType type)
        => type.Name;

    public static implicit operator int(TaskType type)
        => type.Value;

    public class ManualType() : TaskType(nameof(Manual), 0) { }
    public class AutomaticType() : TaskType(nameof(Automatic), 1) { }
    public class ApprovalType() : TaskType(nameof(Approval), 2) { }
}
