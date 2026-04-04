using Core.Domain.Primitives;
using WorkflowTemplate.Domain.Enumerations;

namespace WorkflowTemplate.Domain.Entities;

public class TemplateStepField : Entity
{
    public Guid StepId { get; private set; }
    public string Label { get; private set; } = null!;
    public FieldType FieldType { get; private set; }
    public string? Options { get; private set; }
    public int Order { get; internal set; }

    public TemplateStepField(Guid id, Guid stepId, string label, FieldType fieldType, string? options, int order)
    {
        Id = id;
        StepId = stepId;
        Label = label;
        FieldType = fieldType;
        Options = options;
        Order = order;
    }

    public static TemplateStepField Create(Guid id, Guid stepId, string label, FieldType fieldType, string? options, int order)
        => new(id, stepId, label, fieldType, options, order);

    public void Update(string label, FieldType fieldType, string? options, int order, DateTimeOffset updatedAt)
    {
        Label = label;
        FieldType = fieldType;
        Options = options;
        Order = order;
        UpdatedAt = updatedAt;
    }

    internal void ShiftOrder(int newOrder) => Order = newOrder;
}