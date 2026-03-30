using Core.Persistence.Configurations;
using Identity.Domain.Aggregates;

namespace Identity.Persistence.Configurations;

public class SnapshotConfiguration
{
    public class UserSnapshotConfiguration : SnapshotConfiguration<User>;
    public class UserInviteSnapshotConfiguration : SnapshotConfiguration<UserInvite>;
}