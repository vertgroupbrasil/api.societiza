using Core.Persistence.Configurations;

namespace Accountancy.Persistence.Configurations;

public class StoreEventConfiguration
{
    public class AccountancyStoreEventConfiguration : StoreEventConfiguration<Accountancy.Domain.Aggregates.Accountancy>;
}