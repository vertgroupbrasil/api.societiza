using Core.Application.ServiceLifetimes;
using Identity.Application.Services.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Services.Abstractions;

public sealed class PasswordService : IPasswordService, ITransient
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            HashSize);

        byte[] payload = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, payload, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, payload, SaltSize, HashSize);

        return Convert.ToBase64String(payload);
    }

    public bool VerifyPassword(string password, string hash)
    {
        byte[] payload = Convert.FromBase64String(hash);

        if (payload.Length != SaltSize + HashSize)
            return false;

        byte[] salt = new byte[SaltSize];
        byte[] storedHash = new byte[HashSize];
        Buffer.BlockCopy(payload, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(payload, SaltSize, storedHash, 0, HashSize);

        byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }

}
