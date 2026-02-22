using Core.Application.Services.Interfaces;
using Corporate.Persistence.Context;

namespace Corporate.Application.Services;

public interface ICorporateApplicationService : IApplicationService<CorporateDbContext>
{
}