using Microsoft.EntityFrameworkCore;
using WorkflowProcess.Persistence.Constants;

namespace WorkflowProcess.Persistence.Context;

public sealed class WorkflowProcessDbContext(DbContextOptions<WorkflowProcessDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.WorkflowProcess);
        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
    }
}