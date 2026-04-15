using Core.Infrastructure.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Web.Services.CrossModule;
using WorkflowProcess.Application.Services;

namespace Web.ServiceInstallers.CrossModule;

internal sealed class CrossModuleServicesInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IWorkflowTemplateDataService, WorkflowTemplateDataService>();
    }
}
