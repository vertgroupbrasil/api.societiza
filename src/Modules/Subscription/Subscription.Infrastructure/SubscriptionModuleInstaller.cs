using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Subscription.Infrastructure;

public sealed class SubscriptionModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration) =>
        services.InstallServicesFromAssemblies(
            configuration,
            AssemblyReference.Assembly);
}