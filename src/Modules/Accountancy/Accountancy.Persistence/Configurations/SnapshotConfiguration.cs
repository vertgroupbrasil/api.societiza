using Core.Persistence.Configurations;

namespace Accountancy.Persistence.Configurations;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

public class SnapshotConfiguration
{
    public class AccountancySnapshotConfiguration : SnapshotConfiguration<AccountancyAggregate>;
}