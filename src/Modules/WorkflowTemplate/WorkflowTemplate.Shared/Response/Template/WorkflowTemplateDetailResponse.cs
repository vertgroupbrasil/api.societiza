using WorkflowTemplate.Shared.Response.TemplateStep;

namespace WorkflowTemplate.Shared.Response.Template;

public sealed record WorkflowTemplateDetailResponse(
    Guid Id,
    string Name,
    string Description,
    string Version,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    List<WorkflowTemplateStepDetailResponse> Steps
);