namespace WorkflowTemplate.Shared.Response.TemplateStep;

public sealed record WorkflowTemplateStepFieldDetailResponse(
    Guid Id,
    string Label,
    string FieldType,
    string? Options
);