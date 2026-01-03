using Core.Infrastructure.Configurations;
using Core.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Payment.Application.Services;
using Payment.Infrastructure.Services;
using Payment.Shared.Options;
using System.Net.Http.Headers;

namespace Payment.Infrastructure.ServiceInstallers;

public sealed class InfrastructureServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AbacatePayOptions>()
            .Bind(configuration.GetSection(AbacatePayOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();


        services.AddHttpClient<IPaymentService, PaymentService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AbacatePayOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddScopedAsMatchingInterfaces(AssemblyReference.Assembly);
        services.AddSingletonAsMatchingInterfaces(AssemblyReference.Assembly);
        services.AddTransientAsMatchingInterfaces(AssemblyReference.Assembly);
    }
}