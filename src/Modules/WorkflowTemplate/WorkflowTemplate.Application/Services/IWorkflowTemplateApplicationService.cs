using Core.Application.Services.Interfaces;
using WorkflowTemplate.Persistence.Context;

namespace WorkflowTemplate.Application.Services;

public interface IWorkflowTemplateApplicationService : IApplicationService<WorkflowTemplateDbContext>
{
}