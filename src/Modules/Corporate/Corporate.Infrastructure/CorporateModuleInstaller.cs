using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Corporate.Infrastructure;

public sealed class CorporateModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration) =>
        services.InstallServicesFromAssemblies(
            configuration, 
            AssemblyReference.Assembly);
}