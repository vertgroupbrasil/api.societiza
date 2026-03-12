using Core.Persistence.Configurations;
using UserAggregate = User.Domain.Aggregates.User;

namespace User.Persistence.Configurations;

public class StoreEventConfiguration
{
    public class UserStoreEventConfiguration : StoreEventConfiguration<UserAggregate>;
}