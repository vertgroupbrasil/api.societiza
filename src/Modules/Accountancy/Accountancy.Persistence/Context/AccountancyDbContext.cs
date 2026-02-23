using Accountancy.Persistence.Constants;
using Microsoft.EntityFrameworkCore;

namespace Accountancy.Persistence.Context;

public sealed class AccountancyDbContext(DbContextOptions<AccountancyDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Accountancy);
        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
    }
}