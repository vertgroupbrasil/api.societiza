using Core.Application.EventBus;
using Core.Application.EventStore;
using Core.Application.Services;
using Core.Application.UnitOfWork;
using Workflow.Application.Services;
using Workflow.Persistence.Context;

namespace Workflow.Infrastructure.Services;

public class WorkflowApplicationService(IEventStore<WorkflowDbContext> eventStore, IEventBus eventBusGateway, IUnitOfWork<WorkflowDbContext> unitOfWork)
    : ApplicationService<WorkflowDbContext>(eventStore, eventBusGateway, unitOfWork), IWorkflowApplicationService
{
}