using Core.Domain.Primitives;

namespace WorkflowProcess.Domain.Entities;

public class ProcessStepTaskFieldInstance : Entity
{
    public Guid TaskInstanceId { get; private set; }
    public Guid TemplateStepTaskFieldId { get; private set; }
    public string Label { get; private set; } = null!;
    public string FieldType { get; private set; } = null!;
    public string? Options { get; private set; }
    public bool IsRequired { get; private set; }
    public string? Value { get; private set; }
    public int Order { get; private set; }
    public DateTimeOffset? FilledAt { get; private set; }

    public ProcessStepTaskFieldInstance(
        Guid id,
        Guid taskInstanceId,
        Guid templateStepTaskFieldId,
        string label,
        string fieldType,
        string? options,
        bool isRequired,
        int order)
    {
        Id = id;
        TaskInstanceId = taskInstanceId;
        TemplateStepTaskFieldId = templateStepTaskFieldId;
        Label = label;
        FieldType = fieldType;
        Options = options;
        IsRequired = isRequired;
        Order = order;
    }

    public static ProcessStepTaskFieldInstance Create(
        Guid id,
        Guid taskInstanceId,
        Guid templateStepTaskFieldId,
        string label,
        string fieldType,
        string? options,
        bool isRequired,
        int order)
        => new(id, taskInstanceId, templateStepTaskFieldId, label, fieldType, options, isRequired, order);

    public void Fill(string value, DateTimeOffset filledAt)
    {
        Value = value;
        FilledAt = filledAt;
    }
}
