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
    private const int Iterations = 100_000;

    private readonly ZonedClock _clock = clock.InUtc();
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<IAuthService.LoginResult> AttemptLogin(string username, string suppliedPlaintextPassword)
    {
        await _unitOfWork.BeginTransaction();
        try
        {
            var userResult = await _unitOfWork.UserRepository.GetByUsername(username);
            if (userResult.IsNotFound)
            {
                return new Failure();
            }

            var user = userResult.AsUser();

            if (!IsPasswordValid(user))
            {
                return new Failure();
            }

            var token = _tokenProvider.CreateToken(user);
            UpdateUserRefreshToken(user, token.RefreshToken);

            await _unitOfWork.Commit();

            return new Success<ITokenProvider.NewToken>(token);
        }
        catch (Exception ex)
        {
            await _unitOfWork.Rollback();

            // no logging configured in this auth demo project
            Console.WriteLine(ex.Message);

            throw;
        }

        bool IsPasswordValid(User user)
        {
            var passwordBytes = GetPasswordBytes(suppliedPlaintextPassword);
            var hashedPassword = new HashedPassword(user.PasswordHash, user.PasswordSalt);

            return VerifyPassword(passwordBytes, hashedPassword);
        }

        void UpdateUserRefreshToken(User user, ITokenProvider.TokenData refreshToken)
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
        await _unitOfWork.BeginTransaction();
        try
        {
            var userResult = await _unitOfWork.UserRepository.GetByUsername(username);
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
            await _unitOfWork.Commit();

            return new Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.Rollback();

            // no logging configured in this auth demo project
            Console.WriteLine(ex.Message);

            throw;
        }
    }

    public async ValueTask<IAuthService.LoginResult> AttemptTokenRefresh(string username, string suppliedRefreshToken)
    {
        await _unitOfWork.BeginTransaction();
        try
        {
            var userResult = await _unitOfWork.UserRepository.GetByUsername(username);
            if (userResult.IsNotFound)
            {
                return new Failure();
            }

            var user = userResult.AsUser();

            var refreshResult = AttemptRefresh(user);
            if (refreshResult.IsSuccessOfNewToken)
            {
                await _unitOfWork.Commit();
            }

            return refreshResult;
        }
        catch (Exception ex)
        {
            await _unitOfWork.Rollback();

            // no logging configured in this auth demo project
            Console.WriteLine(ex.Message);

            throw;
        }

        IAuthService.LoginResult AttemptRefresh(User user)
        {
            RemoveExpiredRefreshTokens(user);

            if (!TryFindRefreshToken(user, GetPasswordBytes(suppliedRefreshToken), out var refreshToken))
            {
                return new Failure();
            }

            user.ActiveRefreshTokens.Remove(refreshToken);

            var token = _tokenProvider.CreateToken(user);
            AddNewRefreshToken(user, token.RefreshToken);

            return new Success<ITokenProvider.NewToken>(token);
        }
    }

    private static bool TryFindRefreshToken(User user, ReadOnlySpan<byte> tokenBytes,
                                            out ActiveRefreshToken refreshToken)
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

        refreshToken = default(ActiveRefreshToken)!;

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
