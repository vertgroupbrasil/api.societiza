using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Subscription.Infrastructure.ServiceInstallers;

public sealed class InfrastructureServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScopedAsMatchingInterfaces(AssemblyReference.Assembly);
        services.AddSingletonAsMatchingInterfaces(AssemblyReference.Assembly);
        services.AddTransientAsMatchingInterfaces(AssemblyReference.Assembly);
    }
}