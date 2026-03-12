namespace WorkflowTemplate.Shared.Response.TemplateStepTask;

public sealed record WorkflowTemplateStepTaskDetailResponse(
    Guid Id,
    string Title,
    string Description,
    string Type,
    string Configuration,
    int Order,
    bool IsOptional,
    List<WorkflowTemplateStepTaskFieldDetailResponse> Fields
);