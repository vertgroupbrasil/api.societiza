using Core.Domain.Entities;
using Payment.Domain.Enumerations;

namespace Payment.Domain.Entities;

public class Payment : Entity
{
    public Guid SubscriptionId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public PaymentStatus Status { get; private set; }
    public PaymentMethod? Method { get; private set; }

    public string? AbacatePayCheckoutId { get; private set; }
    public string? AbacatePayPaymentId { get; private set; }

    public DateTimeOffset? PaidAt { get; private set; }
    public string Description { get; private set; } = string.Empty;

    public static Payment CreateForSubscription(Guid subscriptionId, decimal amount, string description)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            Amount = amount,
            Description = description,
            Status = PaymentStatus.Pending
        };
    }

    public void MarkAsPaid(string abacatePayPaymentId, PaymentMethod? paymentMethod = null)
    {
        Status = PaymentStatus.Paid;
        AbacatePayPaymentId = abacatePayPaymentId;
        Method = paymentMethod;
        PaidAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPaymentMethod(PaymentMethod method)
    {
        Method = method;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}