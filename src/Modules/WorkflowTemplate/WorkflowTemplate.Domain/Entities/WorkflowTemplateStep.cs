using Core.Domain.Primitives;
using WorkflowTemplate.Domain.Enumerations;

namespace WorkflowTemplate.Domain.Entities;

public class WorkflowTemplateStep : Entity
{
    public Guid TemplateId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int Order { get; private set; }
    public List<TemplateStepField> Fields { get; private set; } = [];
    public List<WorkflowTemplateStepTask> Tasks { get; private set; } = [];

    public WorkflowTemplateStep(Guid id, Guid templateId, string title, string description, int order)
    {
        Id = id;
        TemplateId = templateId;
        Title = title;
        Description = description;
        Order = order;
    }

    public static WorkflowTemplateStep Create(Guid id, Guid templateId, string title, string description, int order)
        => new(id, templateId, title, description, order);

    public void Update(string title, string description, int order, DateTimeOffset updatedAt)
    {
        Title = title;
        Description = description;
        Order = order;
        UpdatedAt = updatedAt;
    }

    internal void ShiftOrder(int newOrder) => Order = newOrder;

    public void AddTask(WorkflowTemplateStepTask task)
        => Tasks.Add(task);

    public void UpdateTask(Guid taskId, string title, string description, TaskType type, string configuration, int order, bool isOptional, DateTimeOffset updatedAt)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == taskId);
        task?.Update(title, description, type, configuration, order, isOptional, updatedAt);
    }

    public void RemoveTask(Guid taskId)
        => Tasks.RemoveAll(t => t.Id == taskId);

    public void AddField(TemplateStepField field)
        => Fields.Add(field);

    public void UpdateField(Guid fieldId, string label, FieldType fieldType, string? options, int order, DateTimeOffset updatedAt)
    {
        var field = Fields.FirstOrDefault(f => f.Id == fieldId);
        field?.Update(label, fieldType, options, order, updatedAt);
    }

    public void RemoveField(Guid fieldId)
        => Fields.RemoveAll(f => f.Id == fieldId);
}
