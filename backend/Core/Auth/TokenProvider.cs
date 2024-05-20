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
    NewToken CreateToken(User user);

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

        List<Claim> claims =
        [
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.SerialNumber, Guid.NewGuid().ToString())
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
}
