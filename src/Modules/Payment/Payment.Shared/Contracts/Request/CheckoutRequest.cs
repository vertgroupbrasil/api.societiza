using Payment.Domain.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace Payment.Shared.Contracts.Request;

public record CheckoutRequest
{
    [Required]
    public string CheckoutName { get; init; } = string.Empty;
    [Required]
    public string CheckoutDescription { get; init; } = string.Empty;
    [Required]
    public string CustomerPhone { get; init; } = string.Empty;
    [Required]
    public string CustomerEmail { get; init; } = string.Empty;
    [Required]
    public string CustomerName { get; init; } = string.Empty;
    [Required]
    public string CustomerDocument { get; init; } = string.Empty;
    [Required]
    public int AmountInCents { get; init; }
    public PaymentMethod[] AllowedMethods { get; init; } = null!;
}