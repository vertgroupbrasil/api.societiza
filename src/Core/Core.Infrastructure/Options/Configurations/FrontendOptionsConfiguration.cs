using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Core.Infrastructure.Options.Configurations;

internal sealed class FrontendOptionsConfiguration(IConfiguration configuration) : IConfigureOptions<FrontendOptions>
{
    internal const string SectionName = "FrontendOptions";

    public void Configure(FrontendOptions options)
        => configuration.GetSection(SectionName).Bind(options);
}
