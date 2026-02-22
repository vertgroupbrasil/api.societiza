using Core.Persistence.Configurations;
using Corporate.Domain.Aggregates;

namespace Corporate.Persistence.Configurations;

public class StoreEventConfiguration
{
    public class AccountancyStoreEventConfiguration : StoreEventConfiguration<Accountancy>;
}