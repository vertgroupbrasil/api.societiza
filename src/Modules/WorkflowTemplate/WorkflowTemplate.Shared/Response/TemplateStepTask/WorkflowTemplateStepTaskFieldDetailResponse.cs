namespace WorkflowTemplate.Shared.Response.TemplateStepTask;

public sealed record WorkflowTemplateStepTaskFieldDetailResponse(
    Guid Id,
    string Label,
    string FieldType,
    string? Options,
    int Order
);