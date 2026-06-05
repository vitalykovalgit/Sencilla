using Konscious.Security.Cryptography;

namespace Sencilla.Authentication;

/// <summary>
/// Default <see cref="IPasswordHasher"/>. Hashes with Argon2id (PHC string output) using the cost
/// parameters and optional pepper from <see cref="PasswordPolicyOptions"/>. <see cref="Verify"/>
/// additionally accepts two legacy formats — ASP.NET Identity v3 PBKDF2 and the
/// <c>iterations.salt.hash</c> scheme — so existing users log in and are transparently upgraded.
/// </summary>
public sealed class Argon2idPasswordHasher : IPasswordHasher
{
    private const string Argon2Prefix = "$argon2id$";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Argon2Version = 19;

    private readonly PasswordPolicyOptions _options;
    private readonly byte[]? _pepper;

    public Argon2idPasswordHasher(IOptions<PasswordPolicyOptions> options)
    {
        _options = options.Value;
        _pepper = string.IsNullOrEmpty(_options.Pepper) ? null : Encoding.UTF8.GetBytes(_options.Pepper);
    }

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        int m = _options.Argon2MemoryKb, t = _options.Argon2Iterations, p = _options.Argon2Parallelism;
        var hash = Argon2(password, salt, m, t, p, HashSize);
        return $"{Argon2Prefix}v={Argon2Version}$m={m},t={t},p={p}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        try
        {
            if (storedHash.StartsWith(Argon2Prefix, StringComparison.Ordinal))
                return VerifyArgon2(password, storedHash);
            if (LooksLikeIterSaltHash(storedHash))
                return VerifyIterSaltHash(password, storedHash);
            if (TryDecodeBase64(storedHash, out var identityBlob) && identityBlob.Length > 0 && identityBlob[0] == 0x01)
                return VerifyIdentityV3(password, identityBlob);
            return false;
        }
        catch
        {
            // Malformed stored hash never throws to the caller.
            return false;
        }
    }

    public bool NeedsRehash(string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash) || !storedHash.StartsWith(Argon2Prefix, StringComparison.Ordinal))
            return true; // legacy / unknown format → upgrade on next successful login

        var (m, t, p) = ParseArgon2Params(storedHash);
        return m < _options.Argon2MemoryKb || t < _options.Argon2Iterations || p != _options.Argon2Parallelism;
    }

    // ---------- Argon2id ----------

    private byte[] Argon2(string password, byte[] salt, int memoryKb, int iterations, int parallelism, int size)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKb,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };
        if (_pepper is not null)
            argon2.KnownSecret = _pepper;

        return argon2.GetBytes(size);
    }

    private bool VerifyArgon2(string password, string stored)
    {
        // $argon2id$v=19$m=..,t=..,p=..$<b64 salt>$<b64 hash>
        var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
            return false;

        var (m, t, p) = ParseArgon2Params(stored);
        var salt = Convert.FromBase64String(parts[3]);
        var expected = Convert.FromBase64String(parts[4]);
        var actual = Argon2(password, salt, m, t, p, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static (int m, int t, int p) ParseArgon2Params(string stored)
    {
        var seg = stored.Split('$', StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(s => s.StartsWith("m=", StringComparison.Ordinal));
        if (seg is null)
            return (0, 0, 0);

        int m = 0, t = 0, p = 0;
        foreach (var kv in seg.Split(','))
        {
            var i = kv.IndexOf('=');
            if (i < 0)
                continue;
            var key = kv[..i];
            if (!int.TryParse(kv[(i + 1)..], out var val))
                continue;
            switch (key)
            {
                case "m": m = val; break;
                case "t": t = val; break;
                case "p": p = val; break;
            }
        }
        return (m, t, p);
    }

    // ---------- Legacy: "iterations.salt.hash" (PBKDF2-HMAC-SHA256) ----------

    private static bool LooksLikeIterSaltHash(string stored)
    {
        var parts = stored.Split('.');
        return parts.Length == 3 && int.TryParse(parts[0], out _);
    }

    private static bool VerifyIterSaltHash(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    // ---------- Legacy: ASP.NET Identity v3 (0x01 marker, PBKDF2) ----------

    private static bool VerifyIdentityV3(string password, byte[] blob)
    {
        // [0]=0x01, [1..4]=prf, [5..8]=iterations, [9..12]=saltLen (all uint32 big-endian)
        if (blob.Length < 13)
            return false;

        var prf = ReadUInt32BigEndian(blob, 1);
        var iterations = (int)ReadUInt32BigEndian(blob, 5);
        var saltLength = (int)ReadUInt32BigEndian(blob, 9);
        if (saltLength < 8 || 13 + saltLength >= blob.Length)
            return false;

        var algorithm = prf switch
        {
            0 => HashAlgorithmName.SHA1,
            1 => HashAlgorithmName.SHA256,
            2 => HashAlgorithmName.SHA512,
            _ => throw new InvalidOperationException("Unknown PRF"),
        };

        var salt = blob.AsSpan(13, saltLength).ToArray();
        var expected = blob.AsSpan(13 + saltLength).ToArray();
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iterations, algorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static uint ReadUInt32BigEndian(byte[] buffer, int offset)
        => ((uint)buffer[offset] << 24) | ((uint)buffer[offset + 1] << 16)
         | ((uint)buffer[offset + 2] << 8) | buffer[offset + 3];

    private static bool TryDecodeBase64(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        Span<byte> buffer = value.Length <= 1024 ? stackalloc byte[value.Length] : new byte[value.Length];
        if (Convert.TryFromBase64String(value, buffer, out var written))
        {
            bytes = buffer[..written].ToArray();
            return true;
        }
        return false;
    }
}
