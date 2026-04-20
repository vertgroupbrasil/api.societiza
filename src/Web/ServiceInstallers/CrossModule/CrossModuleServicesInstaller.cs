using Core.Infrastructure.Configurations;
using Identity.Application.Services;
using Web.Services.CrossModule;
using WorkflowProcess.Application.Services;

namespace Web.ServiceInstallers.CrossModule;

internal sealed class CrossModuleServicesInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IWorkflowTemplateDataService, WorkflowTemplateDataService>();
        services.AddScoped<IAccountancyDataService, AccountancyDataService>();
    }
}