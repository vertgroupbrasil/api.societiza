using Core.Persistence.Configurations;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

namespace Accountancy.Persistence.Configurations;

public class StoreEventConfiguration
{
    public class AccountancyStoreEventConfiguration : StoreEventConfiguration<AccountancyAggregate>;
}