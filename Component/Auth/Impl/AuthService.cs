using global::Sencilla.Component.Users.Auth;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sencilla.Component.Users.Auth;

public class AuthService : IAuthService
{
    private readonly ICreateRepository<User> _createRepo;
    private readonly IReadRepository<User> _readRepo;
    private readonly ICreateRepository<UserRefreshToken> _createTokenRepo;
    private readonly IReadRepository<UserRefreshToken> _readTokenRepo;

    private readonly AuthOptions _authOptions; // JWT config

    public AuthService(
        ICreateRepository<User> createRepo,
        IReadRepository<User> readRepo,
        ICreateRepository<UserRefreshToken> createTokenRepo,
        IReadRepository<UserRefreshToken> readTokenRepo,
        IOptions<AuthOptions> authOptions
    )
    {
        _createRepo = createRepo;
        _readRepo = readRepo;
        _createTokenRepo = createTokenRepo;
        _readTokenRepo = readTokenRepo;
        _authOptions = authOptions.Value;
    }

    public async Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // 1) Check if user exists 
        var existingUser = (await _readRepo.GetAll(
            UserFilter.ByEmail(request.Email), ct
        )).FirstOrDefault();

        if (existingUser != null)
        {
            throw new Exception("User already registered with that email");
        }

        // 2) Create user
        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHashHelper.HashPassword(request.Password),
            Phone = request.Phone,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        user.AddRole((int)RoleType.User);

        // 3) Upsert to DB
        await _createRepo.UpsertAsync(user, u => u.Email);

        // 4) Return tokens
        return await GenerateTokenResponse(user, ct);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // 1) Find user 
        var user = (await _readRepo.GetAll(
            UserFilter.ByEmail(request.Email), ct
        )).FirstOrDefault();

        if (user == null)
            throw new Exception("No user with that email");

        // 2) Check password
        bool isValid = PasswordHashHelper.VerifyPassword(
            user.PasswordHash,
            request.Password
        );
        if (!isValid)
            throw new Exception("Invalid password");

        // 3) Return tokens
        return await GenerateTokenResponse(user, ct);
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenObj = (await _readTokenRepo.GetAll(
            UserRefreshTokenFilter.ByToken(refreshToken)
        )).FirstOrDefault();

        if (tokenObj is null
            || tokenObj.ExpiresAt < DateTime.UtcNow
            || tokenObj.RevokedAt.HasValue)
        {
            // Invalid or expired token
            throw new Exception("Invalid refresh token");
        }

        // Grab user
        var user = (await _readRepo.GetAll(
            UserFilter.ById(tokenObj.UserId)
        )).FirstOrDefault();
        if (user == null)
            throw new Exception("User no longer exists");

        // Mark old token as “replaced/revoked” 
        tokenObj.RevokedAt = DateTime.UtcNow;
        tokenObj.ReplacedByToken = Guid.NewGuid().ToString();
        await _createTokenRepo.UpsertAsync(tokenObj, x => x.Id);

        // Issue new tokens
        return await GenerateTokenResponse(user, ct);
    }

    private async Task<TokenResponse> GenerateTokenResponse(User user, CancellationToken ct)
    {
        // 1) Build the claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            // add roles, phone, or whatever else
        };

        // 2) Create the JWT 
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_authOptions.SecretKey)
        );
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_authOptions.JwtExpiresMinutes);
        var token = new JwtSecurityToken(
            issuer: _authOptions.Issuer,
            audience: _authOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );
        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // 3) Create a new refresh token 
        var refresh = new UserRefreshToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };
        await _createTokenRepo.UpsertAsync(refresh, x => x.Token);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refresh.Token,
            ExpiresAt = expires
        };
    }
}

