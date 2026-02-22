using Microsoft.Extensions.Options;

namespace Web.ServiceInstallers.EventBus.Options.Configurations;

internal sealed class EventBusOptionsConfiguration(IConfiguration configuration)
    : IConfigureOptions<EventBusOptions>
{
    internal const string ConfigurationSectionName = "EventBusOptions";

    public void Configure(EventBusOptions options)
        => configuration.GetSection(ConfigurationSectionName).Bind(options);
}