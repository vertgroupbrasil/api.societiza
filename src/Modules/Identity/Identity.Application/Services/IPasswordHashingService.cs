namespace Identity.Application.Services;

public interface IPasswordHashingService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}