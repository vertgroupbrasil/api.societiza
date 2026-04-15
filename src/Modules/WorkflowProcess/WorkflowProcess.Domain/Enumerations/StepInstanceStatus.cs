using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace WorkflowProcess.Domain.Enumerations;

[method: JsonConstructor]
public class StepInstanceStatus(string name, int value) : SmartEnum<StepInstanceStatus>(name, value)
{
    public static readonly StepInstanceStatus NotStarted = new NotStartedStatus();
    public static readonly StepInstanceStatus InProgress = new InProgressStatus();
    public static readonly StepInstanceStatus Completed = new CompletedStatus();

    public static implicit operator StepInstanceStatus(string name)
        => FromName(name);

    public static implicit operator StepInstanceStatus(int value)
        => FromValue(value);

    public static implicit operator string(StepInstanceStatus status)
        => status.Name;

    public static implicit operator int(StepInstanceStatus status)
        => status.Value;

    public class NotStartedStatus() : StepInstanceStatus(nameof(NotStarted), 0) { }
    public class InProgressStatus() : StepInstanceStatus(nameof(InProgress), 1) { }
    public class CompletedStatus() : StepInstanceStatus(nameof(Completed), 2) { }
}
