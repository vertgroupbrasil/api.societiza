using Core.Application.Services.Interfaces;
using Identity.Persistence.Context;

namespace Identity.Application.Services;

public interface IIdentityApplicationService : IApplicationService<IdentityDbContext>
{
}