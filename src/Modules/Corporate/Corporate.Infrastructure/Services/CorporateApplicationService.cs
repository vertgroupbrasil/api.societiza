using Core.Application.EventBus;
using Core.Application.EventStore;
using Core.Application.Services;
using Core.Application.UnitOfWork;
using Corporate.Application.Services;
using Corporate.Persistence.Context;

namespace Corporate.Infrastructure.Services;

public class CorporateApplicationService(IEventStore<CorporateDbContext> eventStore, IEventBus eventBusGateway, IUnitOfWork<CorporateDbContext> unitOfWork)
    : ApplicationService<CorporateDbContext>(eventStore, eventBusGateway, unitOfWork), ICorporateApplicationService
{
}