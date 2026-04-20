using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Services.Abstractions;
using UserAggregate = Identity.Domain.Aggregates.User;
using Identity.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Services.Abstractions;

internal sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    private const int AccessTokenExpirationMinutes = 15;
    private const int RefreshTokenExpirationDays = 7;

    public string GenerateAccessToken(UserAggregate user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.Name),
            new("security_stamp", user.SecurityStamp.ToString())
        };

        if (user.AccountancyId.HasValue)
            claims.Add(new("accountancy_id", user.AccountancyId.Value.ToString()));

        return GenerateToken(claims, TimeSpan.FromMinutes(AccessTokenExpirationMinutes));
    }

    public string GenerateRefreshToken(UserAggregate user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return GenerateToken(claims, TimeSpan.FromDays(RefreshTokenExpirationDays));
    }

    public ClaimsPrincipal? ValidateRefreshToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_options.Key)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            return new JwtSecurityTokenHandler { MapInboundClaims = false }
                .ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    private string GenerateToken(IEnumerable<Claim> claims, TimeSpan expiration)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiration),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}