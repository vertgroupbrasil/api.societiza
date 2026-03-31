using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace WorkflowTemplate.Domain.Enumerations;

[method: JsonConstructor]
public class TemplateStatus(string name, int value) : SmartEnum<TemplateStatus>(name, value)
{
    public static readonly TemplateStatus Draft = new DraftStatus();
    public static readonly TemplateStatus Active = new ActiveStatus();
    public static readonly TemplateStatus Archived = new ArchivedStatus();

    public static implicit operator TemplateStatus(string name)
        => FromName(name);

    public static implicit operator TemplateStatus(int value)
        => FromValue(value);

    public static implicit operator string(TemplateStatus status)
        => status.Name;

    public static implicit operator int(TemplateStatus status)
        => status.Value;

    public class DraftStatus() : TemplateStatus(nameof(Draft), 0) { }
    public class ActiveStatus() : TemplateStatus(nameof(Active), 1) { }
    public class ArchivedStatus() : TemplateStatus(nameof(Archived), 2) { }
}
