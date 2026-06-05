namespace Sencilla.Authentication.Jwt.Tests;

public class JwtTokenIssuerTests
{
    private const string SigningKey = "test-signing-key-at-least-32-bytes-long!!";
    private const string Issuer = "embedded-app";
    private const string Audience = "embedded-app-api";

    private static JwtTokenIssuer Build()
    {
        var store = new InMemoryRefreshTokenStore();
        var refresh = new RefreshTokenService(store, Options.Create(new RefreshTokenOptions()));
        return new JwtTokenIssuer(
            Options.Create(new JwtIssuerOptions
            {
                SigningKey = SigningKey,
                Issuer = Issuer,
                Audience = Audience,
                AccessTokenLifetimeMinutes = 10,
            }),
            refresh);
    }

    [Fact]
    public async Task Issue_ProducesValidatableAccessTokenWithSubjectAndRefresh()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AuthClaims.Subject, userId.ToString()),
            new Claim(AuthClaims.Email, "user@example.com"),
        }, authenticationType: "test"));

        var response = await Build().Issue(principal);

        Assert.False(string.IsNullOrEmpty(response.AccessToken));
        Assert.False(string.IsNullOrEmpty(response.RefreshToken));
        Assert.Equal("Bearer", response.TokenType);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(response.AccessToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
        });

        Assert.True(validation.IsValid);
        var jwt = (JsonWebToken)validation.SecurityToken;
        Assert.Equal(userId.ToString(), jwt.Subject);
    }
}
