using Core.Domain.Primitives;
using WorkflowTemplate.Domain.Enumerations;

namespace WorkflowTemplate.Domain.Entities;

public class TemplateStepTaskField : Entity
{
    public Guid TaskId { get; private set; }
    public string Label { get; private set; } = null!;
    public FieldType FieldType { get; private set; }
    public string? Options { get; private set; }
    public int Order { get; private set; }

    public TemplateStepTaskField(Guid id, Guid taskId, string label, FieldType fieldType, string? options, int order)
    {
        Id = id;
        TaskId = taskId;
        Label = label;
        FieldType = fieldType;
        Options = options;
        Order = order;
    }

    public static TemplateStepTaskField Create(Guid id, Guid taskId, string label, FieldType fieldType, string? options, int order)
        => new(id, taskId, label, fieldType, options, order);

    public void Update(string label, FieldType fieldType, string? options, int order, DateTimeOffset updatedAt)
    {
        Label = label;
        FieldType = fieldType;
        Options = options;
        Order = order;
        UpdatedAt = updatedAt;
    }
}
