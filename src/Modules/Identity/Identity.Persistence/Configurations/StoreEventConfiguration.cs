using Core.Persistence.Configurations;

namespace Identity.Persistence.Configurations;

using UserAggregate = Identity.Domain.Aggregates.User;
using InvitationLinkAggregate = Identity.Domain.Aggregates.InvitationLink;

public class StoreEventConfiguration
{
    public class UserStoreEventConfiguration : StoreEventConfiguration<UserAggregate>;
    public class InvitationLinkStoreEventConfiguration : StoreEventConfiguration<InvitationLinkAggregate>;
}