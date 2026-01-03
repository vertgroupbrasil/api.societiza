using Core.Infrastructure.Extensions;
using Core.Persistence.Context;
using CorrelationId.DependencyInjection;
using Finbuckle.MultiTenant;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Reflection;
using Web.Configurations;
using Web.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Services Core
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationId();
builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddControllers()
    .AddApplicationPart(Web.AssemblyReference.Assembly);

// HTTP Logging for Development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging(logging =>
    {
        logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;

        logging.RequestHeaders.Add("X-Tenant-ID");
        logging.RequestHeaders.Add("Authorization");

        logging.ResponseHeaders.Add("Content-Type");
        logging.MediaTypeOptions.AddText("application/json");

        logging.RequestBodyLogLimit = 4096;
        logging.ResponseBodyLogLimit = 4096;
    });
}

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "MeuSocietario API",
        Version = "v1",
        Description = "API for managing corporate processes",
        Contact = new OpenApiContact
        {
            Name = "Vert Contábil",
            Email = "contato@vertgroup.com"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.EnableAnnotations();
    c.OperationFilter<TenantHeaderOperationFilter>();
});

builder.Services
    .InstallServicesFromAssemblies(
        builder.Configuration,
        Web.AssemblyReference.Assembly,
        Core.Persistence.AssemblyReference.Assembly,
        Core.Infrastructure.AssemblyReference.Assembly)
    .InstallModulesFromAssemblies(
        builder.Configuration,
        Identity.Infrastructure.AssemblyReference.Assembly,
        Corporate.Infrastructure.AssemblyReference.Assembly,
        Subscription.Infrastructure.AssemblyReference.Assembly,
        Payment.Infrastructure.AssemblyReference.Assembly);

// Multi-tenant
builder.Services.AddMultiTenant<Core.Domain.Entities.Tenant>()
    .WithEFCoreStore<ApplicationDbContext, Core.Domain.Entities.Tenant>()
    .WithHeaderStrategy("X-Tenant-ID");

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware pipeline
app.UsePathBase("/api");
app.UseRequestTimestamp();
app.UseMiddleware<ExceptionHandlingMiddleware>();

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

app.UseHttpsRedirection();
app.UseMultiTenant();
app.UseCors("AllowAll");

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