using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Infrastructure.ServiceInstallers;

public sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScopedAsMatchingInterfaces(Persistence.AssemblyReference.Assembly);
        services.AddSingletonAsMatchingInterfaces(Persistence.AssemblyReference.Assembly);
        services.AddTransientAsMatchingInterfaces(Persistence.AssemblyReference.Assembly);
    }
}