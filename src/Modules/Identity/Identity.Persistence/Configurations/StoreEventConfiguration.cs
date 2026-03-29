using Core.Persistence.Configurations;
using Identity.Domain.Aggregates;

namespace Identity.Persistence.Configurations;

public class StoreEventConfiguration
{
    public class UserStoreEventConfiguration : StoreEventConfiguration<User>;
    public class UserInviteStoreEventConfiguration : StoreEventConfiguration<UserInvite>;
}