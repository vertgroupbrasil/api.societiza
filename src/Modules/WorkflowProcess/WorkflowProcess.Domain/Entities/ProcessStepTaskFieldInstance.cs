using Core.Domain.Primitives;

namespace WorkflowProcess.Domain.Entities;

public class ProcessStepTaskFieldInstance : Entity
{
    public Guid TaskInstanceId { get; private set; }
    public Guid TemplateStepTaskFieldId { get; private set; }
    public string Label { get; private set; } = null!;
    public string FieldType { get; private set; } = null!;
    public string? Options { get; private set; }
    public string? Value { get; private set; }
    public int Order { get; private set; }
    public DateTimeOffset? FilledAt { get; private set; }

    public ProcessStepTaskFieldInstance(Guid id, Guid taskInstanceId, Guid templateStepTaskFieldId, string label, string fieldType, string? options, int order)
    {
        Id = id;
        TaskInstanceId = taskInstanceId;
        TemplateStepTaskFieldId = templateStepTaskFieldId;
        Label = label;
        FieldType = fieldType;
        Options = options;
        Order = order;
    }

    public static ProcessStepTaskFieldInstance Create(Guid id, Guid taskInstanceId, Guid templateStepTaskFieldId, string label, string fieldType, string? options, int order)
        => new(id, taskInstanceId, templateStepTaskFieldId, label, fieldType, options, order);

    public void Fill(string value, DateTimeOffset filledAt)
    {
        Value = value;
        FilledAt = filledAt;
    }
}