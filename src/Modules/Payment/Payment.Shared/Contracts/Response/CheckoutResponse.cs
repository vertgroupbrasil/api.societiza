using System.Text.Json.Serialization;

namespace Payment.Shared.Contracts.Response;

// Response to create a checkout in the AbacatePay gateway.
public sealed record CheckoutResponse<T>(
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("error")] string? Error
);