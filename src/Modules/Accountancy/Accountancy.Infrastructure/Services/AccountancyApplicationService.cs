using Core.Application.EventBus;
using Core.Application.EventStore;
using Core.Application.Services;
using Core.Application.UnitOfWork;
using Accountancy.Application.Services;
using Accountancy.Persistence.Context;

namespace Accountancy.Infrastructure.Services;

public class AccountancyApplicationService(IEventStore<AccountancyDbContext> eventStore, IEventBus eventBusGateway, IUnitOfWork<AccountancyDbContext> unitOfWork)
    : ApplicationService<AccountancyDbContext>(eventStore, eventBusGateway, unitOfWork), IAccountancyApplicationService
{
}