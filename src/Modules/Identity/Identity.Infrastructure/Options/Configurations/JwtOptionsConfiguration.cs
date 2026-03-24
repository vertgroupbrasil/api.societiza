using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Options.Configurations;

internal sealed class JwtOptionsConfiguration(IConfiguration configuration)
    : IConfigureOptions<JwtOptions>
{
    internal const string SectionName = "JwtOptions";

    public void Configure(JwtOptions options)
        => configuration.GetSection(SectionName).Bind(options);
}