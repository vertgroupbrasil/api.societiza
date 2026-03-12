using Core.Domain.Primitives;
using WorkflowTemplate.Domain.Enumerations;

namespace WorkflowTemplate.Domain.Entities;

public class TemplateStepTaskField : Entity
{
    public Guid TaskId { get; private set; }
    public string Label { get; private set; } = null!;
    public FieldType FieldType { get; private set; }
    public string? Options { get; private set; }

    public TemplateStepTaskField(Guid id, Guid taskId, string label, FieldType fieldType, string? options)
    {
        Id = id;
        TaskId = taskId;
        Label = label;
        FieldType = fieldType;
        Options = options;
    }

    public static TemplateStepTaskField Create(Guid id, Guid taskId, string label, FieldType fieldType, string? options)
        => new(id, taskId, label, fieldType, options);

    public void Update(string label, FieldType fieldType, string? options, DateTimeOffset updatedAt)
    {
        Label = label;
        FieldType = fieldType;
        Options = options;
        UpdatedAt = updatedAt;
    }
}