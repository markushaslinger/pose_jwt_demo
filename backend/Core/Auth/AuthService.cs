using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using JwtDemo.Core.Users;
using NodaTime;
using NodaTime.Extensions;
using UnionGen;
using UnionGen.Types;

namespace JwtDemo.Core.Auth;

public partial interface IAuthService
{
    public ValueTask<LoginResult> AttemptLogin(string username, string suppliedPlaintextPassword);
    public ValueTask<LoginResult> AttemptTokenRefresh(string username, string suppliedRefreshToken);
    public HashedPassword HashPassword(string plaintext);
    public ValueTask<LogoutResult> AttemptLogout(string requestUsername, string requestRefreshToken);

    [Union<Success<ITokenProvider.NewToken>, NotFound, Failure>]
    public readonly partial struct LoginResult;

    [Union<Success, NotFound>]
    public readonly partial struct LogoutResult;

    public sealed record HashedPassword(byte[] PasswordHash, byte[] Salt);
}

internal sealed class AuthService(ITokenProvider tokenProvider, IClock clock, IUnitOfWork unitOfWork) : IAuthService
{
    private const int KeySize = 64;
    private const int Iterations = 200_000;

    private readonly ZonedClock _clock = clock.InUtc();

    public async ValueTask<IAuthService.LoginResult> AttemptLogin(string username, string suppliedPlaintextPassword)
    {
        var userResult = await unitOfWork.UserRepository.GetByUsername(username);
        if (userResult.IsNotFound)
        {
            return new Failure();
        }

        var user = userResult.AsUser();

        if (!IsPasswordValid())
        {
            return new Failure();
        }

        var token = tokenProvider.CreateToken(user);
        UpdateUserRefreshToken(token.RefreshToken);

        return new Success<ITokenProvider.NewToken>(token);

        bool IsPasswordValid()
        {
            var passwordBytes = GetPasswordBytes(suppliedPlaintextPassword);
            var hashedPassword = new HashedPassword(user.PasswordHash, user.PasswordSalt);

            return VerifyPassword(passwordBytes, hashedPassword);
        }

        void UpdateUserRefreshToken(ITokenProvider.TokenData refreshToken)
        {
            RemoveExpiredRefreshTokens(user);
            AddNewRefreshToken(user, refreshToken);
        }
    }

    public IAuthService.HashedPassword HashPassword(string plaintext)
    {
        var hashedPassword = HashPassword(GetPasswordBytes(plaintext));

        return new IAuthService.HashedPassword(hashedPassword.PasswordHash.ToArray(), hashedPassword.Salt.ToArray());
    }

    public async ValueTask<IAuthService.LogoutResult> AttemptLogout(string username, string refreshToken)
    {
        // we could also create a blocklist to revoke the access token, but given the short expiration time,
        // we won't bother with that added complexity this time
        var userResult = await unitOfWork.UserRepository.GetByUsername(username);
        if (userResult.IsNotFound)
        {
            return new NotFound();
        }

        var user = userResult.AsUser();
        if (!TryFindRefreshToken(user, GetPasswordBytes(refreshToken), out var activeRefreshToken))
        {
            return new NotFound();
        }

        user.ActiveRefreshTokens.Remove(activeRefreshToken);

        return new Success();
    }

    public async ValueTask<IAuthService.LoginResult> AttemptTokenRefresh(string username, string suppliedRefreshToken)
    {
        var userResult = await unitOfWork.UserRepository.GetByUsername(username);
        if (userResult.IsNotFound)
        {
            return new Failure();
        }

        var user = userResult.AsUser();
        var refreshResult = AttemptRefresh();

        return refreshResult;

        IAuthService.LoginResult AttemptRefresh()
        {
            RemoveExpiredRefreshTokens(user);

            if (!TryFindRefreshToken(user, GetPasswordBytes(suppliedRefreshToken), out var refreshToken))
            {
                return new Failure();
            }

            user.ActiveRefreshTokens.Remove(refreshToken);

            var token = tokenProvider.CreateToken(user);
            AddNewRefreshToken(user, token.RefreshToken);

            return new Success<ITokenProvider.NewToken>(token);
        }
    }

    private static bool TryFindRefreshToken(User user, ReadOnlySpan<byte> tokenBytes,
                                            [NotNullWhen(true)] out ActiveRefreshToken? refreshToken)
    {
        foreach (var activeRefreshToken in user.ActiveRefreshTokens)
        {
            var hashedProvidedToken = HashPassword(tokenBytes, activeRefreshToken.TokenSalt);
            if (hashedProvidedToken.SequenceEqual(activeRefreshToken.TokenHash))
            {
                refreshToken = activeRefreshToken;

                return true;
            }
        }

        refreshToken = null;

        return false;
    }

    private static void AddNewRefreshToken(User user, ITokenProvider.TokenData refreshToken)
    {
        var hashedRefreshToken = HashPassword(GetPasswordBytes(refreshToken.Token));
        user.ActiveRefreshTokens.Add(new ActiveRefreshToken
        {
            Expiration = refreshToken.Expiration,
            TokenHash = hashedRefreshToken.PasswordHash.ToArray(),
            TokenSalt = hashedRefreshToken.Salt.ToArray()
        });
    }

    private static HashedPassword HashPassword(ReadOnlySpan<byte> plaintext)
    {
        ReadOnlySpan<byte> salt = RandomNumberGenerator.GetBytes(KeySize);
        var hash = HashPassword(plaintext, salt);

        return new HashedPassword(hash, salt);
    }

    private static bool VerifyPassword(ReadOnlySpan<byte> plaintext, HashedPassword hashedPassword)
    {
        var hashToCompare = HashPassword(plaintext, hashedPassword.Salt);

        return hashToCompare.SequenceEqual(hashedPassword.PasswordHash);
    }

    private void RemoveExpiredRefreshTokens(User user)
    {
        var now = _clock.GetCurrentInstant();
        user.ActiveRefreshTokens.RemoveAll(token => token.Expiration < now);
    }

    private static ReadOnlySpan<byte> GetPasswordBytes(string plaintext) => Encoding.UTF8.GetBytes(plaintext);

    private static ReadOnlySpan<byte> HashPassword(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> salt) =>
        Rfc2898DeriveBytes.Pbkdf2(plaintext, salt,
                                  Iterations, HashAlgorithmName.SHA512, KeySize);

    private readonly ref struct HashedPassword(ReadOnlySpan<byte> passwordHash, ReadOnlySpan<byte> salt)
    {
        public ReadOnlySpan<byte> PasswordHash { get; } = passwordHash;
        public ReadOnlySpan<byte> Salt { get; } = salt;
    }
}
