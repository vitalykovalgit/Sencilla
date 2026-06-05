namespace Sencilla.Authentication;

/// <summary>
/// Password hashing seam. The default Argon2id implementation is registered by
/// <c>UseAppAccounts</c>, so validation-only (bearer) consumers pull no cryptography.
/// <see cref="Verify"/> accepts legacy formats (ASP.NET Identity PBKDF2, the
/// <c>iterations.salt.hash</c> scheme) and <see cref="NeedsRehash"/> drives rehash-on-login.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hash a plaintext password using the current algorithm and parameters.</summary>
    string Hash(string password);

    /// <summary>Verify a plaintext password against a stored hash of any supported format.</summary>
    bool Verify(string password, string storedHash);

    /// <summary>True when the stored hash uses a weaker format or parameters than the current policy.</summary>
    bool NeedsRehash(string storedHash);
}
