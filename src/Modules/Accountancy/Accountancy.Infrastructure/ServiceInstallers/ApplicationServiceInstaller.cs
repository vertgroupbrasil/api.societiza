using Core.Application.Behaviors;
using Core.Infrastructure.Configurations;
using Core.Shared.Extensions;
using Accountancy.Application.Services;
using Accountancy.Infrastructure.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Accountancy.Application.DependencyInjection;

namespace Accountancy.Infrastructure.ServiceInstallers;

internal sealed class ApplicationServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.Tap(services.TryAddTransient<IAccountancyApplicationService, AccountancyApplicationService>);
        services.AddEventInteractors();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly);

            config.AddOpenBehavior(typeof(RequestLoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);
    }
}