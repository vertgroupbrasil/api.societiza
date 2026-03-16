using Core.Application.Behaviors;
using Core.Infrastructure.Configurations;
using Core.Shared.Extensions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WorkflowTemplate.Application.DependencyInjection;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Infrastructure.Services;
using WorkflowTemplate.Application.UseCases.Events;

namespace WorkflowTemplate.Infrastructure.ServiceInstallers;

internal sealed class ApplicationServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.Tap(services.TryAddTransient<IWorkflowTemplateApplicationService, WorkflowTemplateApplicationService>);
        services.AddEventInteractors();
        services.AddTransient<IProjectionWorkflowTemplateCascadeDeleteEventHandler, ProjectionWorkflowTemplateCascadeDeleteEventHandler>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly);

            config.AddOpenBehavior(typeof(RequestLoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        services.AddAutoMapper(Application.AssemblyReference.Assembly);
        services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);
    }
}