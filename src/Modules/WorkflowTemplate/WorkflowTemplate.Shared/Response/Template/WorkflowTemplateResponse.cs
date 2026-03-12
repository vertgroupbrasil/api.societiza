namespace WorkflowTemplate.Shared.Response.Template;

public sealed record WorkflowTemplateResponse(
    Guid Id,
    string Name,
    string Description,
    string Version,
    string Status,
    DateTimeOffset CreatedAt
);