using Core.Persistence.Configurations;
using UserAggregate = User.Domain.Aggregates.User;

namespace User.Persistence.Configurations;

public class SnapshotConfiguration
{
    public class UserSnapshotConfiguration : SnapshotConfiguration<UserAggregate>;
}