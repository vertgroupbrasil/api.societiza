using Core.Application.EventStore;
using Core.Application.UnitOfWork;
using Core.Infrastructure.Configurations;
using Core.Persistence.EventStore;
using Core.Persistence.Extensions;
using Core.Persistence.Options;
using Core.Persistence.UnitOfWork;
using Accountancy.Persistence.Constants;
using Accountancy.Persistence.Context;
using Accountancy.Persistence.Context.Interfaces;
using Accountancy.Persistence.Projection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Accountancy.Infrastructure.ServiceInstallers;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Register Projection Repository
        services.AddScoped(typeof(IAccountancyProjection<>), typeof(AccountancyProjection<>));

        // Register Event Store
        services.AddScoped(typeof(IEventStore<AccountancyDbContext>), typeof(EventStore<AccountancyDbContext>));

        // Register Unit of Work
        services.AddScoped(typeof(IUnitOfWork<AccountancyDbContext>), typeof(UnitOfWork<AccountancyDbContext>));

        services.AddScoped<IAccountancyProjectionDbContext>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            string connectionString = configuration.GetSection("Projections").GetValue<string>("Accountancy");

            return new AccountancyProjectionDbContext(connectionString);
        })
        .AddDbContext<AccountancyDbContext>((provider, builder) =>
        {
            ConnectionStringOptions connectionString = provider.GetService<IOptions<ConnectionStringOptions>>()!.Value;

            builder.UseNpgsql(
                connectionString: connectionString,
                dbContextOptionsBuilder => dbContextOptionsBuilder.WithMigrationHistoryTableInSchema(Schemas.Accountancy));
        });
    }
}