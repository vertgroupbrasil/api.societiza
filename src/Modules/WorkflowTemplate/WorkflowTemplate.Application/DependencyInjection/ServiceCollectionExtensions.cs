using Microsoft.Extensions.DependencyInjection;
using WorkflowTemplate.Application.UseCases.Events;

namespace WorkflowTemplate.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventInteractors(this IServiceCollection services)
        => services
            .AddScoped<IProjectionWorkflowTemplateEventHandler, ProjectionWorkflowTemplateEventHandler>()
            .AddScoped<IProjectionWorkflowTemplateStepEventHandler, ProjectionWorkflowTemplateStepEventHandler>()
            .AddScoped<IProjectionWorkflowTemplateStepTaskEventHandler, ProjectionWorkflowTemplateStepTaskEventHandler>()
            .AddScoped<IProjectionTemplateStepFieldEventHandler, ProjectionTemplateStepFieldEventHandler>()
            .AddScoped<IProjectionTemplateStepTaskFieldEventHandler, ProjectionTemplateStepTaskFieldEventHandler>();
}