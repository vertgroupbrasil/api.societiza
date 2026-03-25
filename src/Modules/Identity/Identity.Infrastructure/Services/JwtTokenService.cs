using Core.Application.ServiceLifetimes;
using Identity.Application.Services;
using Identity.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService, ITransient
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string GeneratePasswordResetToken(Guid Id, string email)
    {
        ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[]
        {
            new Claim("Id", Id.ToString()),
            new Claim("Email", email),
            new Claim("purpose", "password_reset")
        });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateJwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            subject: claimsIdentity,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return tokenHandler.WriteToken(token);
    }

    public (Guid UserId, string Email)? ValidatePasswordResetToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var tokenHandler = new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var purpose = jwtToken.Claims.FirstOrDefault(c => c.Type == "purpose")?.Value;

            if (purpose != "password_reset")
                return null;

            var userIdStr = jwtToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "Email")?.Value;

            if (!Guid.TryParse(userIdStr, out var userId) || email is null)
                return null;

            return (userId, email);
        }
        catch
        {
            return null;
        }
    }
}