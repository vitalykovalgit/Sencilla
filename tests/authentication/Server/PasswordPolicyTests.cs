using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Sencilla.Authentication.Server.Tests;

public class PasswordPolicyTests
{
    private static readonly PasswordPolicyOptions Strict = new() { MinLength = 8, MaxLength = 16, RequireDigit = true, RequireMixedCase = true };

    [Fact]
    public void TooShort_IsItsOwnCode()
    {
        var ex = Assert.Throws<BadRequestException>(() => PasswordPolicy.Validate("Ab1", Strict));
        Assert.Equal("password-too-short", ex.Message);
    }

    [Theory]
    [InlineData("Abcdefghijklmnopq1")]   // over MaxLength
    [InlineData("Abcdefghij")]           // no digit
    [InlineData("abcdefgh1")]            // no upper case
    [InlineData("ABCDEFGH1")]            // no lower case
    public void PolicyViolations_ReportPasswordPolicy(string password)
    {
        var ex = Assert.Throws<BadRequestException>(() => PasswordPolicy.Validate(password, Strict));
        Assert.Equal("password-policy", ex.Message);
    }

    [Fact]
    public void CompliantPassword_Passes()
    {
        PasswordPolicy.Validate("Picalbums1", Strict);
    }

    [Fact]
    public void DefaultOptions_OnlyEnforceLength()
    {
        PasswordPolicy.Validate("all-lower-no-digits", new PasswordPolicyOptions());
    }

    [Fact]
    public void TheEndpoint_AnswersTheFourRules_AndNeverThePepperOrTheArgon2Cost()
    {
        var policy = new PasswordPolicyOptions { MinLength = 8, MaxLength = 64, RequireDigit = true, RequireMixedCase = true, Pepper = "secret-pepper", Argon2Iterations = 7 };
        using var limiter = new AuthRateLimiter(Options.Create(new AuthApiOptions()));

        var result = new AppAccountsEndpoint(Options.Create(new AuthApiOptions()), limiter, Options.Create(policy)).GetPasswordPolicy();

        var body = JsonSerializer.Serialize(((IValueHttpResult)result).Value, JsonSerializerOptions.Web);
        Assert.Equal("""{"minLength":8,"maxLength":64,"requireDigit":true,"requireMixedCase":true}""", body);
        Assert.DoesNotContain("pepper", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("argon2", body, StringComparison.OrdinalIgnoreCase);
    }
}
