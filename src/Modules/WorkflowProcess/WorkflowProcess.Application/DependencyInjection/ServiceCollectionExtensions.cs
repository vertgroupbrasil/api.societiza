using Microsoft.Extensions.DependencyInjection;
using WorkflowProcess.Application.UseCases.Events.Process;
using WorkflowProcess.Application.UseCases.Events.ProcessStep;
using WorkflowProcess.Application.UseCases.Events.ProcessStepTask;

namespace WorkflowProcess.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventInteractors(this IServiceCollection services)
        => services
            .AddScoped<IProjectionWorkflowProcessEventHandler, ProjectionWorkflowProcessEventHandler>()
            .AddScoped<IProjectionProcessStepInstanceEventHandler, ProjectionProcessStepInstanceEventHandler>()
            .AddScoped<IProjectionProcessStepTaskInstanceEventHandler, ProjectionProcessStepTaskInstanceEventHandler>()
            .AddScoped<IProjectionProcessStepFieldInstanceEventHandler, ProjectionProcessStepFieldInstanceEventHandler>()
            .AddScoped<IProjectionProcessStepTaskFieldInstanceEventHandler, ProjectionProcessStepTaskFieldInstanceEventHandler>();
}