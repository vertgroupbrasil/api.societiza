using Payment.Shared.Contracts.Request;
using Payment.Shared.Contracts.Response;
using Payment.Shared.Contracts.Response.DTOs;

namespace Payment.Application.Services;

public interface IPaymentService
{
    Task<CheckoutResponse<CheckoutCreatedDto>> CreateCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken);
}