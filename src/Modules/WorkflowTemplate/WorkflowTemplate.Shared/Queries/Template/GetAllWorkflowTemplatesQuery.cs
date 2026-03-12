using Core.Application.Messaging.Queries;
using WorkflowTemplate.Shared.Response.Template;

namespace WorkflowTemplate.Shared.Queries.Template;

public sealed record GetAllWorkflowTemplatesQuery() : IQuery<List<WorkflowTemplateResponse>>;