using Core.Application.ServiceLifetimes;
using Identity.Application.Services;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Services;

public sealed class PasswordService : IPasswordService, ITransient
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private const int RandomPasswordLength = 12;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    private static readonly char[] UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] LowercaseChars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
    private static readonly char[] DigitChars = "0123456789".ToCharArray();
    private static readonly char[] SpecialChars = "!@#$%&*".ToCharArray();
    private static readonly char[] AllChars = [.. UppercaseChars, .. LowercaseChars, .. DigitChars, .. SpecialChars];

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

    public string GenerateRandomPassword()
    {
        char[] password = new char[RandomPasswordLength];

        password[0] = UppercaseChars[RandomNumberGenerator.GetInt32(UppercaseChars.Length)];
        password[1] = LowercaseChars[RandomNumberGenerator.GetInt32(LowercaseChars.Length)];
        password[2] = DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)];
        password[3] = SpecialChars[RandomNumberGenerator.GetInt32(SpecialChars.Length)];

        for (int i = 4; i < RandomPasswordLength; i++)
            password[i] = AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)];

        RandomNumberGenerator.Shuffle(password.AsSpan());

        return new string(password);
    }
}
