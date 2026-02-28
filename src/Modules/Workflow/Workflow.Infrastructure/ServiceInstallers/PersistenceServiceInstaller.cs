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
using Workflow.Persistence.Constants;
using Workflow.Persistence.Context;
using Workflow.Persistence.Context.Interfaces;
using Workflow.Persistence.Projection;

namespace Workflow.Infrastructure.ServiceInstallers;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Register Projection Repository
        services.AddScoped(typeof(IWorkflowProjection<>), typeof(WorkflowProjection<>));

        // Register Event Store
        services.AddScoped(typeof(IEventStore<WorkflowDbContext>), typeof(EventStore<WorkflowDbContext>));

        // Register Unit of Work
        services.AddScoped(typeof(IUnitOfWork<WorkflowDbContext>), typeof(UnitOfWork<WorkflowDbContext>));

        services.AddScoped<IWorkflowProjectionDbContext>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            string connectionString = configuration.GetSection("Projections").GetValue<string>("Workflow");

            return new WorkflowProjectionDbContext(connectionString);
        })
        .AddDbContext<WorkflowDbContext>((provider, builder) =>
        {
            ConnectionStringOptions connectionString = provider.GetService<IOptions<ConnectionStringOptions>>()!.Value;

            builder.UseNpgsql(
                connectionString: connectionString,
                dbContextOptionsBuilder => dbContextOptionsBuilder.WithMigrationHistoryTableInSchema(Schemas.Workflow));
        });
    }
}