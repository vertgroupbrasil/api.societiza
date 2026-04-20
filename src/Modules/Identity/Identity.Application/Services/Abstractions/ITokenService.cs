using System.Security.Claims;

namespace Identity.Application.Services.Abstractions;

using UserAggregate = Identity.Domain.Aggregates.User;

public interface ITokenService
{
    string GenerateAccessToken(UserAggregate user);
    string GenerateRefreshToken(UserAggregate user);
    ClaimsPrincipal? ValidateRefreshToken(string token);
}