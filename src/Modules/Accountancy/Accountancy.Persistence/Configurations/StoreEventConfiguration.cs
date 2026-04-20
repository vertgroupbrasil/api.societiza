using Core.Persistence.Configurations;

namespace Accountancy.Persistence.Configurations;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

public class StoreEventConfiguration
{
    public class AccountancyStoreEventConfiguration : StoreEventConfiguration<AccountancyAggregate>;
}