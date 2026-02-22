using Core.Persistence.Configurations;
using Corporate.Domain.Aggregates;

namespace Corporate.Persistence.Configurations;

public class SnapshotConfiguration
{
    public class AccountancySnapshotConfiguration : SnapshotConfiguration<Accountancy>;
}