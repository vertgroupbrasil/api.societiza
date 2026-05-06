using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace WorkflowProcess.Domain.Enumerations;

[method: JsonConstructor]
public class ProcessStatus(string name, int value) : SmartEnum<ProcessStatus>(name, value)
{
    public static readonly ProcessStatus InProgress = new InProgressStatus();
    public static readonly ProcessStatus Completed = new CompletedStatus();

    public static implicit operator ProcessStatus(string name)
        => FromName(name);

    public static implicit operator ProcessStatus(int value)
        => FromValue(value);

    public static implicit operator string(ProcessStatus status)
        => status.Name;

    public static implicit operator int(ProcessStatus status)
        => status.Value;

    public class InProgressStatus() : ProcessStatus(nameof(InProgress), 0) { }
    public class CompletedStatus() : ProcessStatus(nameof(Completed), 1) { }
}