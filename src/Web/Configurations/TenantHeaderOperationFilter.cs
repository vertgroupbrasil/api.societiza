using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Web.Attributes;

namespace Web.Configurations;

public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Checks if the endpoint has AllowAnonymous or NoTenantRequired attribute
        var hasAllowAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        var hasNoTenantRequired = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<NoTenantRequiredAttribute>()
            .Any();

        // If the endpoint requires tenant (not anonymous and not explicitly marked), add the required header
        if (!hasAllowAnonymous && !hasNoTenantRequired)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Tenant-ID",
                In = ParameterLocation.Header,
                Description = "Tenant identifier for multi-tenant operations",
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });
        }

        // If the endpoint is public (has AllowAnonymous), do nothing
    }
}