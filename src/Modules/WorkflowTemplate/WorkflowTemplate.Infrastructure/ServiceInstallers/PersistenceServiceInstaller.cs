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
using WorkflowTemplate.Persistence.Constants;
using WorkflowTemplate.Persistence.Context;
using WorkflowTemplate.Persistence.Context.Interfaces;
using WorkflowTemplate.Persistence.Projection;

namespace WorkflowTemplate.Infrastructure.ServiceInstallers;

internal sealed class PersistenceServiceInstaller : IServiceInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        // Register Projection Repository
        services.AddScoped(typeof(IWorkflowTemplateProjection<>), typeof(WorkflowTemplateProjection<>));

        // Register Event Store
        services.AddScoped(typeof(IEventStore<WorkflowTemplateDbContext>), typeof(EventStore<WorkflowTemplateDbContext>));

        // Register Unit of Work
        services.AddScoped(typeof(IUnitOfWork<WorkflowTemplateDbContext>), typeof(UnitOfWork<WorkflowTemplateDbContext>));

        services.AddScoped<IWorkflowTemplateProjectionDbContext>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            string connectionString = configuration.GetSection("Projections").GetValue<string>("WorkflowTemplate");

            return new WorkflowTemplateProjectionDbContext(connectionString);
        })
        .AddDbContext<WorkflowTemplateDbContext>((provider, builder) =>
        {
            ConnectionStringOptions connectionString = provider.GetService<IOptions<ConnectionStringOptions>>()!.Value;

            builder.UseNpgsql(
                connectionString: connectionString,
                dbContextOptionsBuilder => dbContextOptionsBuilder.WithMigrationHistoryTableInSchema(Schemas.WorkflowTemplate));
        });
    }
}