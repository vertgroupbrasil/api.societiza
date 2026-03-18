using Core.Application.EventBus;
using Core.Application.EventStore;
using Core.Application.Services;
using Core.Application.UnitOfWork;
using Identity.Application.Services;
using Identity.Persistence.Context;

namespace Identity.Infrastructure.Services;

public class IdentityApplicationService(IEventStore<IdentityDbContext> eventStore, IEventBus eventBusGateway, IUnitOfWork<IdentityDbContext> unitOfWork)
    : ApplicationService<IdentityDbContext>(eventStore, eventBusGateway, unitOfWork), IIdentityApplicationService
{
}