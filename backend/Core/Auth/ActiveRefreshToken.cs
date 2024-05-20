using JwtDemo.Core.Users;
using NodaTime;

namespace JwtDemo.Core.Auth;

// this would be much nicer as an owned entity in a JSON column, but we have
// to wait for complex types being enabled for JSON (maybe EF 9, oder 10)
public class ActiveRefreshToken
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public required byte[] TokenHash { get; set; }
    public required byte[] TokenSalt { get; set; }
    public required Instant Expiration { get; set; }
}
