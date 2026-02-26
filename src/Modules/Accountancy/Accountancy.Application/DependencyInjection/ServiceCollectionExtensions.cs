using Accountancy.Application.UseCases.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Accountancy.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventInteractors(this IServiceCollection services)
        => services.AddScoped<IProjectionAccountancyEventHandler, ProjectionAccountancyEventHandler>();
}