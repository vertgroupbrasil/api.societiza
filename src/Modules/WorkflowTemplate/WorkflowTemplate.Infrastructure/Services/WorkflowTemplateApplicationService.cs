using Core.Application.EventBus;
using Core.Application.EventStore;
using Core.Application.Services;
using Core.Application.UnitOfWork;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Persistence.Context;

namespace WorkflowTemplate.Infrastructure.Services;

public class WorkflowTemplateApplicationService(IEventStore<WorkflowTemplateDbContext> eventStore, IEventBus eventBusGateway, IUnitOfWork<WorkflowTemplateDbContext> unitOfWork)
    : ApplicationService<WorkflowTemplateDbContext>(eventStore, eventBusGateway, unitOfWork), IWorkflowTemplateApplicationService
{
}