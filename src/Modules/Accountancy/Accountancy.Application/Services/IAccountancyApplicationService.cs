using Accountancy.Persistence.Context;
using Core.Application.Services.Interfaces;

namespace Accountancy.Application.Services;

public interface IAccountancyApplicationService : IApplicationService<AccountancyDbContext>
{
}