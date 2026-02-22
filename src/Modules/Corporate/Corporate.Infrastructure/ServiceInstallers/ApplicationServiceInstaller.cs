using Core.Application.Behaviors;
using Core.Infrastructure.Configurations;
using Core.Shared.Extensions;
using Corporate.Application.Services;
using Corporate.Infrastructure.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Corporate.Infrastructure.ServiceInstallers;

internal sealed class ApplicationServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.Tap(services.TryAddTransient<ICorporateApplicationService, CorporateApplicationService>);
        //services.AddEventInteractors();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly);

            config.AddOpenBehavior(typeof(RequestLoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        services.AddAutoMapper(Application.AssemblyReference.Assembly);
        services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);
    }
}