using System.Text;
using Asp.Versioning;
using Core.Infrastructure.Extensions;
using DotNetEnv;
using Identity.Application.Authorization;
using Identity.Application.Policies;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Web.Extensions;
using Web.StartupTasks;

// Load environment variables (in order: .env first, then .env.local for overrides)
if (File.Exists(".env"))
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Services Core
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationId();

builder.Services
    .AddControllers()
    .AddApplicationPart(Web.AssemblyReference.Assembly);

// HTTP Logging for Development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging(logging =>
    {
        logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;

        logging.RequestHeaders.Add("Authorization");
        logging.ResponseHeaders.Add("Content-Type");

        logging.MediaTypeOptions.AddText("application/json");

        logging.RequestBodyLogLimit = 4096;
        logging.ResponseBodyLogLimit = 4096;
    });
}

if (builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<SeedDevelopmentDataStartupTask>();

builder.Services
    .InstallServicesFromAssemblies(
        builder.Configuration,
        Web.AssemblyReference.Assembly,
        Core.Infrastructure.AssemblyReference.Assembly,
        Core.Persistence.AssemblyReference.Assembly)
    .InstallModulesFromAssemblies(
        builder.Configuration,
        Accountancy.Infrastructure.AssemblyReference.Assembly,
        Identity.Infrastructure.AssemblyReference.Assembly,
        WorkflowTemplate.Infrastructure.AssemblyReference.Assembly);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.MapInboundClaims = false;

        var jwtOptions = builder.Services
            .BuildServiceProvider() //NOSONAR
            .GetRequiredService<IOptions<JwtOptions>>().Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sub = context.Principal?.FindFirst("sub")?.Value;
                var stamp = context.Principal?.FindFirst("security_stamp")?.Value;

                if (sub is null || stamp is null || !Guid.TryParse(sub, out var userId))
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var appService = context.HttpContext.RequestServices
                    .GetRequiredService<IIdentityApplicationService>();

                var userResult = await appService.LoadAggregateAsync<User>(
                    userId, context.HttpContext.RequestAborted);

                if (!userResult.IsSuccess || userResult.Value.SecurityStamp.ToString() != stamp)
                    context.Fail("Token has been invalidated.");
            }
        };
    });

builder.Services.AddAuthorization(IdentityPolicies.Register);
builder.Services.AddTransient<IAuthorizationHandler, HasRoleHandler>();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    options.AddPolicy("AllowConfigured", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"));
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

var app = builder.Build();

// Middleware pipeline
app.UseMiddlewares();

// Add HTTP loggin for development
if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serilog configuration
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown");
        diagnosticContext.Set("TenantId", httpContext.Request.Headers["X-Tenant-ID"].FirstOrDefault() ?? "None");
    };
});

app.UseCors("AllowConfigured");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    await app.RunAsync();
    Log.Information("Application stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during application startup");
    await app.StopAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
    await app.DisposeAsync();
}