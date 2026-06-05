namespace Sencilla.Authentication.Server.Tests;

public class Argon2idPasswordHasherTests
{
    // Low cost keeps the suite fast; behavior is identical to production parameters.
    private static Argon2idPasswordHasher Hasher(string? pepper = null) => new(Options.Create(new PasswordPolicyOptions
    {
        Argon2MemoryKb = 8192,
        Argon2Iterations = 1,
        Argon2Parallelism = 1,
        Pepper = pepper,
    }));

    [Fact]
    public void Hash_ProducesArgon2idPhcString()
    {
        var hash = Hasher().Hash("password");
        Assert.StartsWith("$argon2id$v=19$", hash);
    }

    [Fact]
    public void Hash_IsSaltedAndDiffersEachCall()
    {
        var hasher = Hasher();
        Assert.NotEqual(hasher.Hash("password"), hasher.Hash("password"));
    }

    [Fact]
    public void Verify_TrueForCorrectPassword()
    {
        var hasher = Hasher();
        var hash = hasher.Hash("correct horse battery staple");
        Assert.True(hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_FalseForWrongPassword()
    {
        var hasher = Hasher();
        var hash = hasher.Hash("correct-password");
        Assert.False(hasher.Verify("wrong-password", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("$argon2id$v=19$m=8192,t=1,p=1$bad$bad")]
    [InlineData("100000.not-base64.also-bad")]
    public void Verify_FalseForMalformedHash(string malformed)
    {
        Assert.False(Hasher().Verify("any", malformed));
    }

    [Fact]
    public void NeedsRehash_FalseForFreshHash()
    {
        var hasher = Hasher();
        Assert.False(hasher.NeedsRehash(hasher.Hash("password")));
    }

    [Fact]
    public void NeedsRehash_TrueForWeakerParameters()
    {
        // Hash produced at lower cost than the verifying hasher's current policy.
        var weak = Hasher().Hash("password");
        var strong = new Argon2idPasswordHasher(Options.Create(new PasswordPolicyOptions
        {
            Argon2MemoryKb = 19_456, Argon2Iterations = 3, Argon2Parallelism = 1,
        }));
        Assert.True(strong.NeedsRehash(weak));
    }

    // ---------- legacy formats ----------

    [Fact]
    public void Verify_TrueForLegacyIterSaltHashFormat()
    {
        const string password = "legacy-pbkdf2";
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
        var stored = $"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

        Assert.True(Hasher().Verify(password, stored));
        Assert.False(Hasher().Verify("wrong", stored));
    }

    [Fact]
    public void Verify_TrueForAspNetIdentityV3Format()
    {
        const string password = "legacy-identity";
        var stored = BuildIdentityV3(password);

        Assert.True(Hasher().Verify(password, stored));
        Assert.False(Hasher().Verify("wrong", stored));
    }

    [Fact]
    public void NeedsRehash_TrueForLegacyFormats()
    {
        var hasher = Hasher();
        Assert.True(hasher.NeedsRehash(BuildIdentityV3("x")));
        Assert.True(hasher.NeedsRehash("100000.AAAA.AAAA"));
    }

    // ---------- pepper ----------

    [Fact]
    public void Pepper_HashMadeWithPepperFailsVerificationWithout()
    {
        var withPepper = Hasher(pepper: "server-side-secret");
        var hash = withPepper.Hash("password");

        Assert.True(withPepper.Verify("password", hash));
        Assert.False(Hasher(pepper: null).Verify("password", hash));
        Assert.False(Hasher(pepper: "different-secret").Verify("password", hash));
    }

    private static string BuildIdentityV3(string password, int iterations = 10_000)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, 32);
        var blob = new byte[13 + salt.Length + subkey.Length];
        blob[0] = 0x01;
        WriteUInt32BigEndian(blob, 1, 1);                  // prf = SHA256
        WriteUInt32BigEndian(blob, 5, (uint)iterations);
        WriteUInt32BigEndian(blob, 9, (uint)salt.Length);
        salt.CopyTo(blob, 13);
        subkey.CopyTo(blob, 13 + salt.Length);
        return Convert.ToBase64String(blob);
    }

    private static void WriteUInt32BigEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}
