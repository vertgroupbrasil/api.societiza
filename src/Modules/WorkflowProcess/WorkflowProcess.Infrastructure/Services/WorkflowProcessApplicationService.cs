using Core.Application.EventBus;
using Core.Application.EventStore;
using Core.Application.Services;
using Core.Application.UnitOfWork;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Persistence.Context;

namespace WorkflowProcess.Infrastructure.Services;

public class WorkflowProcessApplicationService(IEventStore<WorkflowProcessDbContext> eventStore, IEventBus eventBusGateway, IUnitOfWork<WorkflowProcessDbContext> unitOfWork)
    : ApplicationService<WorkflowProcessDbContext>(eventStore, eventBusGateway, unitOfWork), IWorkflowProcessApplicationService
{
}