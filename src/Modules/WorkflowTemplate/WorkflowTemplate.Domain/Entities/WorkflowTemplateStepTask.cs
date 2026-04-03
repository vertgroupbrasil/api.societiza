using Core.Domain.Primitives;
using WorkflowTemplate.Domain.Enumerations;

namespace WorkflowTemplate.Domain.Entities;

public class WorkflowTemplateStepTask : Entity
{
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public TaskType Type { get; private set; }
    public string Configuration { get; private set; } = "{}";
    public int Order { get; private set; }
    public bool IsOptional { get; private set; }
    public List<TemplateStepTaskField> Fields { get; private set; } = [];

    public WorkflowTemplateStepTask(Guid id, string title, string description, TaskType type, string configuration, int order, bool isOptional)
    {
        Id = id;
        Title = title;
        Description = description;
        Type = type;
        Configuration = configuration;
        Order = order;
        IsOptional = isOptional;
    }

    public static WorkflowTemplateStepTask Create(Guid id, string title, string description, TaskType type, string configuration, int order, bool isOptional)
        => new(id, title, description, type, configuration, order, isOptional);

    public void Update(string title, string description, TaskType type, string configuration, int order, bool isOptional, DateTimeOffset updatedAt)
    {
        Title = title;
        Description = description;
        Type = type;
        Configuration = configuration;
        Order = order;
        IsOptional = isOptional;
        UpdatedAt = updatedAt;
    }

    internal void ShiftOrder(int newOrder) => Order = newOrder;

    public void AddField(TemplateStepTaskField field)
        => Fields.Add(field);

    public void UpdateField(Guid fieldId, string label, FieldType fieldType, string? options, DateTimeOffset updatedAt)
    {
        var field = Fields.FirstOrDefault(f => f.Id == fieldId);
        field?.Update(label, fieldType, options, updatedAt);
    }

    public void RemoveField(Guid fieldId)
        => Fields.RemoveAll(f => f.Id == fieldId);
}
