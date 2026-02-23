using Core.Persistence.Configurations;

namespace Accountancy.Persistence.Configurations;

public class SnapshotConfiguration
{
    public class AccountancySnapshotConfiguration : SnapshotConfiguration<Accountancy.Domain.Aggregates.Accountancy>;
}