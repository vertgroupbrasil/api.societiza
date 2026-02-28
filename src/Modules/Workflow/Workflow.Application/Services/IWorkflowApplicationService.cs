using Core.Application.Services.Interfaces;
using Workflow.Persistence.Context;

namespace Workflow.Application.Services;

public interface IWorkflowApplicationService : IApplicationService<WorkflowDbContext>
{
}