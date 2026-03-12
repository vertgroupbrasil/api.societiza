using Microsoft.EntityFrameworkCore;
using WorkflowTemplate.Persistence.Constants;

namespace WorkflowTemplate.Persistence.Context;

public sealed class WorkflowTemplateDbContext(DbContextOptions<WorkflowTemplateDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.WorkflowTemplate);
        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
    }
}