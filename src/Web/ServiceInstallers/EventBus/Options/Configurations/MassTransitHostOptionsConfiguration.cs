using MassTransit;
using Microsoft.Extensions.Options;

namespace Web.ServiceInstallers.EventBus.Options.Configurations;

internal sealed class MassTransitHostOptionsConfiguration(IConfiguration configuration)
    : IConfigureOptions<MassTransitHostOptions>
{
    internal const string ConfigurationSectionName = "MassTransitHostOptions";

    public void Configure(MassTransitHostOptions options)
        => configuration.GetSection(ConfigurationSectionName).Bind(options);
}