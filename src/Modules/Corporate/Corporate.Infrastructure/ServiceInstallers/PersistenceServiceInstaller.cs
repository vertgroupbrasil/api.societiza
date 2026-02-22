using Core.Application.EventStore;
using Core.Application.UnitOfWork;
using Core.Infrastructure.Configurations;
using Core.Persistence.EventStore;
using Core.Persistence.Extensions;
using Core.Persistence.Options;
using Core.Persistence.UnitOfWork;
using Corporate.Persistence.Constants;
using Corporate.Persistence.Context;
using Corporate.Persistence.Context.Interfaces;
using Corporate.Persistence.Projection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Corporate.Infrastructure.ServiceInstallers;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Register Projection Repository
        services.AddScoped(typeof(ICorporateProjection<>), typeof(CorporateProjection<>));

        // Register Event Store
        services.AddScoped(typeof(IEventStore<CorporateDbContext>), typeof(EventStore<CorporateDbContext>));

        // Register Unit of Work
        services.AddScoped(typeof(IUnitOfWork<CorporateDbContext>), typeof(UnitOfWork<CorporateDbContext>));

        services.AddScoped<ICorporateProjectionDbContext>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            string connectionString = configuration.GetSection("Projections").GetValue<string>("Corporate");

            return new CorporateProjectionDbContext(connectionString);
        })
        .AddDbContext<CorporateDbContext>((provider, builder) =>
        {
            ConnectionStringOptions connectionString = provider.GetService<IOptions<ConnectionStringOptions>>()!.Value;

            builder.UseNpgsql(
                connectionString: connectionString,
                dbContextOptionsBuilder => dbContextOptionsBuilder.WithMigrationHistoryTableInSchema(Schemas.Corporate));
        });
    }
}