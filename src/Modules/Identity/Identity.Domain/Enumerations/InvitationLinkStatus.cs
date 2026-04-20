using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace Identity.Domain.Enumerations;

[method: JsonConstructor]
public class InvitationLinkStatus(string name, int value) : SmartEnum<InvitationLinkStatus>(name, value)
{
    public static readonly InvitationLinkStatus Active    = new ActiveStatus();
    public static readonly InvitationLinkStatus Expired   = new ExpiredStatus();
    public static readonly InvitationLinkStatus Revoked   = new RevokedStatus();
    public static readonly InvitationLinkStatus Exhausted = new ExhaustedStatus();

    public static implicit operator InvitationLinkStatus(string name)
        => FromName(name);

    public static implicit operator InvitationLinkStatus(int value)
        => FromValue(value);

    public static implicit operator string(InvitationLinkStatus status)
        => status.Name;

    public static implicit operator int(InvitationLinkStatus status)
        => status.Value;

    public class ActiveStatus()    : InvitationLinkStatus(nameof(Active),    0) { }
    public class ExpiredStatus()   : InvitationLinkStatus(nameof(Expired),   1) { }
    public class RevokedStatus()   : InvitationLinkStatus(nameof(Revoked),   2) { }
    public class ExhaustedStatus() : InvitationLinkStatus(nameof(Exhausted), 3) { }
}