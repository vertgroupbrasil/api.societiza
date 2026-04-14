using System.Security.Claims;
using Identity.Domain.Aggregates;

namespace Identity.Application.Services.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);
    ClaimsPrincipal? ValidateRefreshToken(string token);
}