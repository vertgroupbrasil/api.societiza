using Core.Application.Services.Interfaces;
using Core.Infrastructure.Configurations;
using Core.Infrastructure.Options.Configurations;
using Core.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure;

internal sealed class InfrastructureServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services
            .ConfigureOptions<EmailOptionsConfiguration>()
            .ConfigureOptions<FrontendOptionsConfiguration>()
            .AddScoped<IEmailService, EmailService>();
    }
}