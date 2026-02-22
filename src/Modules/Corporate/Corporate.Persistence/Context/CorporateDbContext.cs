using Corporate.Persistence.Constants;
using Microsoft.EntityFrameworkCore;

namespace Corporate.Persistence.Context;

public sealed class CorporateDbContext(DbContextOptions<CorporateDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Corporate);
        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
    }
}