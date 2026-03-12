using Core.Application.Messaging.Queries;
using WorkflowTemplate.Shared.Response.Template;

namespace WorkflowTemplate.Shared.Queries.Template;

public sealed record GetWorkflowTemplateDetailByIdQuery(Guid Id) : IQuery<WorkflowTemplateDetailResponse>;