using Core.Persistence.Configurations;

namespace Identity.Persistence.Configurations;

using UserAggregate = Identity.Domain.Aggregates.User;
using InvitationLinkAggregate = Identity.Domain.Aggregates.InvitationLink;

public class SnapshotConfiguration
{
    public class UserSnapshotConfiguration : SnapshotConfiguration<UserAggregate>;
    public class InvitationLinkSnapshotConfiguration : SnapshotConfiguration<InvitationLinkAggregate>;
}