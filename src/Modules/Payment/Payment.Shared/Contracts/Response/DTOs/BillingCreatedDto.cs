using System.Text.Json.Serialization;

namespace Payment.Shared.Contracts.Response.DTOs;

// DTO that reflects the API response to create a new checkout in the AbacatePay gateway.
public sealed record CheckoutCreatedDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("status")] string Status
);