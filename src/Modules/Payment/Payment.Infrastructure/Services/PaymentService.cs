using Core.Application.ServiceLifetimes;
using Core.Shared.Extensions;
using Core.Shared.Options;
using Microsoft.Extensions.Logging;
using Payment.Application.Services;
using Payment.Shared.Contracts.Request;
using Payment.Shared.Contracts.Response;
using Payment.Shared.Contracts.Response.DTOs;
using Payment.Shared.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Payment.Infrastructure.Services;

public class PaymentService : IPaymentService, IScoped
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentService> _logger;

    private readonly FrontendOptions _frontendOptions;
    private readonly AbacatePayOptions _abacatePayOptions;

    public PaymentService(
        HttpClient httpClient, 
        ILogger<PaymentService> logger, 
        
        FrontendOptions frontendOptions,
        AbacatePayOptions abacatePayOptions)
    {
        _httpClient = httpClient;
        _logger = logger;

        _frontendOptions = frontendOptions;
        _abacatePayOptions = abacatePayOptions;
    }

    public async Task<CheckoutResponse<CheckoutCreatedDto>> CreateCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!request.CustomerDocument.IsValidCnpj())
            throw new ArgumentException("Invalid customer document (CNPJ).", nameof(request.CustomerDocument));

        if (request.AmountInCents < 100)
            throw new ArgumentOutOfRangeException(nameof(request.AmountInCents), "Minimum is 100 cents (R$1,00).");

        _logger.LogInformation("Creating checkout for {CustomerName}: R${Amount}", request.CustomerName, request.AmountInCents / 100.0);

        var payload = new
        {
            frequency = "ONE_TIME",
            methods = request.AllowedMethods?.Select(m => m.ToString().ToUpperInvariant())
                 ?? new[] { "PIX" },
            products = new[]
            {
                new
                {
                    externalId = Guid.NewGuid().ToString(),
                    name = request.CheckoutName,
                    description = request.CheckoutDescription,
                    price = request.AmountInCents,
                    quantity = 1
                }
            },
            returnUrl = $"{_frontendOptions.BaseUrl}/billing/callback",
            completionUrl = $"{_frontendOptions.BaseUrl}/billing/success",
            customer = new
            {
                name = request.CustomerName,
                cellphone = request.CustomerPhone,
                email = request.CustomerEmail,
                taxId = request.CustomerDocument
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "/billing/create",
            payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<CheckoutResponse<CheckoutCreatedDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        if (envelope is null)
            throw new InvalidOperationException("Empty response from provider.");

        if (!string.IsNullOrWhiteSpace(envelope.Error) || envelope.Data is null)
            throw new InvalidOperationException($"Error: {envelope.Error}");

        return envelope;
    }
}