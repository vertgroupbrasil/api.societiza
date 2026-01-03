using Core.Infrastructure.Configurations;
using Core.Domain.Enumerations;
using Core.Persistence.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Core.Persistence.UnitOfWork;
using Core.Infrastructure.Extensions;
using Core.Persistence.Context;

namespace Core.Persistence;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddScoped<IUnitOfWork<ApplicationDbContext>, UnitOfWork<ApplicationDbContext>>()
            .AddEntityFramework(configuration, DatabaseProvider.PostgreSQL)
            .AddScopedAsMatchingInterfaces(AssemblyReference.Assembly);
    }
}