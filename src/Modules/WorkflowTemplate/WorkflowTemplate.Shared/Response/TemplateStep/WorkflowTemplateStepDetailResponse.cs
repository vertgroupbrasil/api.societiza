using WorkflowTemplate.Shared.Response.TemplateStepTask;

namespace WorkflowTemplate.Shared.Response.TemplateStep;

public sealed record WorkflowTemplateStepDetailResponse(
    Guid Id,
    string Title,
    string Description,
    int Order,
    List<string> AppliesTo,
    List<WorkflowTemplateStepFieldDetailResponse> Fields,
    List<WorkflowTemplateStepTaskDetailResponse> Tasks
);