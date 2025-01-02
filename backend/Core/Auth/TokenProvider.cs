using System.Collections.Frozen;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtDemo.Core.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using NodaTime.Extensions;

namespace JwtDemo.Core.Auth;

public interface ITokenProvider
{
    public NewToken CreateToken(User user);

    public sealed record TokenData(string Token, TokenType Type, Instant Expiration);

    public sealed record NewToken(TokenData AccessToken, TokenData RefreshToken);
}

public enum TokenType
{
    AccessToken,
    RefreshToken
}

internal sealed class TokenProvider(IOptions<AuthSettings> options, IClock clock) : ITokenProvider
{
    private const int RefreshTokenLength = 32;
    private static readonly FrozenDictionary<UserRole, IReadOnlyCollection<Claim>> roleClaims = GetRoleClaims();
    private readonly ZonedClock _clock = clock.InUtc();
    private readonly AuthSettings _settings = options.Value;

    public ITokenProvider.NewToken CreateToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_settings.TokenSigningKey))
        {
            throw new InvalidOperationException("Token signing key is missing");
        }

        var now = _clock.GetCurrentInstant();

        var accessToken = CreateAccessToken(user, now);
        var refreshToken = CreateRefreshToken(now);

        return new ITokenProvider.NewToken(accessToken, refreshToken);
    }

    private ITokenProvider.TokenData CreateAccessToken(User user, Instant now)
    {
        var expiration = now.Plus(Duration.FromMinutes(_settings.AccessTokenLifetimeMinutes));

        IEnumerable<Claim> claims =
        [
            new(ClaimTypes.Name, user.Username),
            ..roleClaims[user.Role],
            new(ClaimTypes.SerialNumber, Guid.NewGuid().ToString("N"))
        ];

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration.ToDateTimeUtc(),
            NotBefore = now.ToDateTimeUtc(),
            SigningCredentials
                = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.TokenSigningKey)),
                                         SecurityAlgorithms.HmacSha256Signature),
            Issuer = _settings.TokenIssuer,
            Audience = _settings.TokenAudience
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        var tokenStr = handler.WriteToken(token);

        return new ITokenProvider.TokenData(tokenStr, TokenType.AccessToken, expiration);
    }

    private ITokenProvider.TokenData CreateRefreshToken(Instant now)
    {
        var expiration = now.Plus(Duration.FromMinutes(_settings.RefreshTokenLifetimeMinutes));

        var buffer = RandomNumberGenerator.GetBytes(RefreshTokenLength);
        var tokenStr = Convert.ToBase64String(buffer);

        return new ITokenProvider.TokenData(tokenStr, TokenType.RefreshToken, expiration);
    }

    private static FrozenDictionary<UserRole, IReadOnlyCollection<Claim>> GetRoleClaims()
    {
        // In this basic authorization model, we assume a hierarchy of roles.
        // So an Admin may do everything a User can, but not vice versa.
        // A real world application might want to constrain more finely - that would then require more 
        // complex logic when matching policies.

        IReadOnlyList<Claim> guest = [Create(UserRole.Guest)];
        IReadOnlyList<Claim> user = [..guest, Create(UserRole.User)];
        IReadOnlyList<Claim> admin = [..user, Create(UserRole.Admin)];
        var claims = new Dictionary<UserRole, IReadOnlyCollection<Claim>>
        {
            [UserRole.Guest] = guest,
            [UserRole.User] = user,
            [UserRole.Admin] = admin
        };

        return claims.ToFrozenDictionary();

        static Claim Create(UserRole r)
        {
            var enumStr = Enum.GetName(r)
                          ?? throw new ArgumentOutOfRangeException(nameof(r), r, "Unknown role");

            return new Claim(ClaimTypes.Role, enumStr);
        }
    }
}
