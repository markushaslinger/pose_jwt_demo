using NodaTime;

namespace JwtDemo.Core.Auth;

// This would be much nicer as an owned entity in a JSON column, but we have to wait for
// complex type collections to be supported: https://github.com/dotnet/efcore/issues/31237
public class ActiveRefreshToken
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public required byte[] TokenHash { get; set; }
    public required byte[] TokenSalt { get; set; }
    public required Instant Expiration { get; set; }
}
