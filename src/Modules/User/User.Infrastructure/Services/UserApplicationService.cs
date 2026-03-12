using Core.Application.EventBus;
using Core.Application.EventStore;
using Core.Application.Services;
using Core.Application.UnitOfWork;
using User.Application.Services;
using User.Persistence.Context;

namespace User.Infrastructure.Services;

public class UserApplicationService(IEventStore<UserDbContext> eventStore, IEventBus eventBusGateway, IUnitOfWork<UserDbContext> unitOfWork)
    : ApplicationService<UserDbContext>(eventStore, eventBusGateway, unitOfWork), IUserApplicationService
{
}