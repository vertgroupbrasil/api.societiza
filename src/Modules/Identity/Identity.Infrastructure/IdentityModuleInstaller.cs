using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public sealed class IdentityModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.InstallServicesFromAssemblies(configuration, AssemblyReference.Assembly);

        services
            .AddTransientAsMatchingInterfaces(AssemblyReference.Assembly)
            .AddScopedAsMatchingInterfaces(AssemblyReference.Assembly);

        services
            .AddTransientAsMatchingInterfaces(Persistence.AssemblyReference.Assembly)
            .AddScopedAsMatchingInterfaces(Persistence.AssemblyReference.Assembly);
    }
}