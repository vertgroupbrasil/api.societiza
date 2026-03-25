namespace Identity.Application.Services;

public interface IJwtTokenService
{
    string GeneratePasswordResetToken(Guid Id, string email);
    (Guid UserId, string Email)? ValidatePasswordResetToken(string token);
}