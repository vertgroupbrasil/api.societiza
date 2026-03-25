using Core.Application.EventBus;
using Core.Infrastructure.Configurations;
using Core.Infrastructure.EventBus;
using Identity.Infrastructure.Options.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Identity.Infrastructure.ServiceInstallers;

internal sealed class InfrastructureServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureOptions<JwtOptionsConfiguration>();

        services.TryAddTransient<IEventBus, EventBus>();
    }
}