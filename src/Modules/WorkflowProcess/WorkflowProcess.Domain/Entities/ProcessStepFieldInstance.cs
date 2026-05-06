using Core.Domain.Primitives;

namespace WorkflowProcess.Domain.Entities;

public class ProcessStepFieldInstance : Entity
{
    public Guid StepInstanceId { get; private set; }
    public Guid TemplateStepFieldId { get; private set; }
    public string Label { get; private set; } = null!;
    public string FieldType { get; private set; } = null!;
    public string? Options { get; private set; }
    public string? Value { get; private set; }
    public int Order { get; private set; }
    public DateTimeOffset? FilledAt { get; private set; }

    public ProcessStepFieldInstance(Guid id, Guid stepInstanceId, Guid templateStepFieldId, string label, string fieldType, string? options, int order)
    {
        Id = id;
        StepInstanceId = stepInstanceId;
        TemplateStepFieldId = templateStepFieldId;
        Label = label;
        FieldType = fieldType;
        Options = options;
        Order = order;
    }

    public static ProcessStepFieldInstance Create(Guid id, Guid stepInstanceId, Guid templateStepFieldId, string label, string fieldType, string? options, int order)
        => new(id, stepInstanceId, templateStepFieldId, label, fieldType, options, order);

    public void Fill(string value, DateTimeOffset filledAt)
    {
        Value = value;
        FilledAt = filledAt;
    }
}