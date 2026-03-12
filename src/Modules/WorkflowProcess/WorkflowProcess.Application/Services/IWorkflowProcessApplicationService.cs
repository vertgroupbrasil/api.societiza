using Core.Application.Services.Interfaces;
using WorkflowProcess.Persistence.Context;

namespace WorkflowProcess.Application.Services;

public interface IWorkflowProcessApplicationService : IApplicationService<WorkflowProcessDbContext>
{
}