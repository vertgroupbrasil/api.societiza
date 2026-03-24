using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Core.Infrastructure.Options.Configurations;

internal sealed class EmailOptionsConfiguration(IConfiguration configuration) : IConfigureOptions<EmailOptions>
{
    internal const string SectionName = "EmailOptions";

    public void Configure(EmailOptions options)
        => configuration.GetSection(SectionName).Bind(options);
}