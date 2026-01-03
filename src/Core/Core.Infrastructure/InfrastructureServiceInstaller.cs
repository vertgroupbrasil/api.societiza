using Core.Infrastructure.Configurations;
using Core.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure;

internal sealed class InfrastructureServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FrontendOptions>()
            .Bind(configuration.GetSection(FrontendOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}