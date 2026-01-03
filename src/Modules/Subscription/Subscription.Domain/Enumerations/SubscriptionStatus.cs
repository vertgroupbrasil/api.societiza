namespace Subscription.Domain.Enumerations;

public enum SubscriptionStatus
{
    Trial, 
    Active, 
    PastDue, 
    Suspended, 
    Canceled, 
    Expired
}