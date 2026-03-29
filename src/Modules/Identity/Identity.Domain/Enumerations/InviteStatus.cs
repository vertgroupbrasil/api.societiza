using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace Identity.Domain.Enumerations;

[method:JsonConstructor]
public class InviteStatus(string name, int value) : SmartEnum<InviteStatus>(name, value)
{
    public static readonly InviteStatus Pending = new PendingStatus();
    public static readonly InviteStatus Accepted = new AcceptedStatus();
    public static readonly InviteStatus Expired = new ExpiredStatus();
    public static readonly InviteStatus Revoked = new RevokedStatus();

    public static implicit operator InviteStatus(string name) 
        => FromName(name);

    public static implicit operator InviteStatus(int value) 
        => FromValue(value);

    public static implicit operator string(InviteStatus status) 
        => status.Name;

    public static implicit operator int(InviteStatus status) 
        => status.Value;

    public class PendingStatus() : InviteStatus(nameof(Pending), 0) { }
    public class AcceptedStatus() : InviteStatus(nameof(Accepted), 1) { }
    public class ExpiredStatus() : InviteStatus(nameof(Expired), 2) { }
    public class RevokedStatus() : InviteStatus(nameof(Revoked), 3) { }
}