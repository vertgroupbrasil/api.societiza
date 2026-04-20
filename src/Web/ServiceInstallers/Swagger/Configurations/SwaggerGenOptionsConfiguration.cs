using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Web.ServiceInstallers.Swagger.Configurations;

internal sealed class SwaggerGenOptionsConfiguration(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.EnableAnnotations();

        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Societiza API",
                Version = description.ApiVersion.ToString(),
                Description = "API for managing corporate processes",
                Contact = new OpenApiContact
                {
                    Name = "Vert Accounting",
                    Email = "contato@vertaccounting.com"
                }
            });
        }

        options.CustomSchemaIds(type => type.ToString().Replace("+", "."));

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token."
        };

        options.AddSecurityDefinition("Bearer", securityScheme);

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    }
}