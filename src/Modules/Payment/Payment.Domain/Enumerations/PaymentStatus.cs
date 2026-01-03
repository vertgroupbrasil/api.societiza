namespace Payment.Domain.Enumerations;

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4,
    Canceled = 5
}