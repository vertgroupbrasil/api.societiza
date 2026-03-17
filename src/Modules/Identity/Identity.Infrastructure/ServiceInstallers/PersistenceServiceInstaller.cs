using Core.Application.EventStore;
using Core.Application.UnitOfWork;
using Core.Infrastructure.Configurations;
using Core.Persistence.EventStore;
using Core.Persistence.Extensions;
using Core.Persistence.Options;
using Core.Persistence.UnitOfWork;
using Identity.Persistence.Constants;
using Identity.Persistence.Context;
using Identity.Persistence.Context.Interfaces;
using Identity.Persistence.Projection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.ServiceInstallers;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Register Projection Repository
        services.AddScoped(typeof(IIdentityProjection<>), typeof(IdentityProjection<>));

        // Register Event Store
        services.AddScoped(typeof(IEventStore<IdentityDbContext>), typeof(EventStore<IdentityDbContext>));

        // Register Unit of Work
        services.AddScoped(typeof(IUnitOfWork<IdentityDbContext>), typeof(UnitOfWork<IdentityDbContext>));

        services.AddScoped<IIdentityProjectionDbContext>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            string connectionString = configuration.GetSection("Projections").GetValue<string>("WorkflowTemplate");

            return new IdentityProjectionDbContext(connectionString);
        })
        .AddDbContext<IdentityDbContext>((provider, builder) =>
        {
            ConnectionStringOptions connectionString = provider.GetService<IOptions<ConnectionStringOptions>>()!.Value;

            builder.UseNpgsql(
                connectionString: connectionString,
                dbContextOptionsBuilder => dbContextOptionsBuilder.WithMigrationHistoryTableInSchema(Schemas.Identity));
        });
    }
}