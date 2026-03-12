using Core.Application.Services.Interfaces;
using User.Persistence.Context;

namespace User.Application.Services;

public interface IUserApplicationService : IApplicationService<UserDbContext>
{
}