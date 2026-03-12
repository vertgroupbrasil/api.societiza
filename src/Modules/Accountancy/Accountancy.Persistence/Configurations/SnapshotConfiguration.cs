using Core.Persistence.Configurations;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

namespace Accountancy.Persistence.Configurations;

public class SnapshotConfiguration
{
    public class AccountancySnapshotConfiguration : SnapshotConfiguration<AccountancyAggregate>;
}