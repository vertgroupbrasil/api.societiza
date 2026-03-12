using Core.Application.EventStore;
using Core.Application.UnitOfWork;
using Core.Infrastructure.Configurations;
using Core.Persistence.EventStore;
using Core.Persistence.Extensions;
using Core.Persistence.Options;
using Core.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using User.Persistence.Constants;
using User.Persistence.Context;
using User.Persistence.Context.Interfaces;
using User.Persistence.Projection;

namespace User.Infrastructure.ServiceInstallers;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Register Projection Repository
        services.AddScoped(typeof(IUserProjection<>), typeof(UserProjection<>));

        // Register Event Store
        services.AddScoped(typeof(IEventStore<UserDbContext>), typeof(EventStore<UserDbContext>));

        // Register Unit of Work
        services.AddScoped(typeof(IUnitOfWork<UserDbContext>), typeof(UnitOfWork<UserDbContext>));

        services.AddScoped<IUserProjectionDbContext>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            string connectionString = configuration.GetSection("Projections").GetValue<string>("WorkflowTemplate");

            return new UserProjectionDbContext(connectionString);
        })
        .AddDbContext<UserDbContext>((provider, builder) =>
        {
            ConnectionStringOptions connectionString = provider.GetService<IOptions<ConnectionStringOptions>>()!.Value;

            builder.UseNpgsql(
                connectionString: connectionString,
                dbContextOptionsBuilder => dbContextOptionsBuilder.WithMigrationHistoryTableInSchema(Schemas.User));
        });
    }
}