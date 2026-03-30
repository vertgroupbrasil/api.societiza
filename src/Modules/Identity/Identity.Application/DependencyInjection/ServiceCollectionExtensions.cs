using Identity.Application.UseCases.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventInteractors(this IServiceCollection services)
        => services
            .AddScoped<IProjectionUserEventHandler, ProjectionUserEventHandler>()
            .AddScoped<IProjectionUserInviteEventHandler, ProjectionUserInviteEventHandler>();
}