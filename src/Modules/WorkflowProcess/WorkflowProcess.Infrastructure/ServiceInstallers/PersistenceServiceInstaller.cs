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
using WorkflowProcess.Persistence.Constants;
using WorkflowProcess.Persistence.Context;
using WorkflowProcess.Persistence.Context.Interfaces;
using WorkflowProcess.Persistence.Projection;

namespace WorkflowProcess.Infrastructure.ServiceInstallers;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IWorkflowProcessProjection<>), typeof(WorkflowProcessProjection<>));

        services.AddScoped(typeof(IEventStore<WorkflowProcessDbContext>), typeof(EventStore<WorkflowProcessDbContext>));

        services.AddScoped(typeof(IUnitOfWork<WorkflowProcessDbContext>), typeof(UnitOfWork<WorkflowProcessDbContext>));

        services.AddScoped<IWorkflowProcessProjectionDbContext>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            string connectionString = configuration.GetSection("Projections").GetValue<string>("WorkflowProcess");

            return new WorkflowProcessProjectionDbContext(connectionString);
        })
        .AddDbContext<WorkflowProcessDbContext>((provider, builder) =>
        {
            ConnectionStringOptions connectionString = provider.GetService<IOptions<ConnectionStringOptions>>()!.Value;

            builder.UseNpgsql(
                connectionString: connectionString,
                dbContextOptionsBuilder => dbContextOptionsBuilder.WithMigrationHistoryTableInSchema(Schemas.WorkflowProcess));
        });
    }
}
