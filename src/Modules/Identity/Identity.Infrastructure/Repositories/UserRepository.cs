using Core.Application.ServiceLifetimes;
using Identity.Domain.Repositories;

namespace Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository, IScoped
{
}