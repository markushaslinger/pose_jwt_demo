using JwtDemo.Core.Auth;
using UnionGen;

namespace JwtDemo.Core.Users;

public partial interface IUserService
{
    public ValueTask<AddUserResult> AddNewUser(string username, string password, UserRole role);
    public ValueTask<GetUserResult> GetByUsername(string username);

    [Union<User, DuplicateUsername>]
    public readonly partial struct AddUserResult;

    public readonly record struct DuplicateUsername;
}

public sealed class UserService(IUnitOfWork unitOfWork, IAuthService authService) : IUserService
{
    public async ValueTask<IUserService.AddUserResult> AddNewUser(string username, string password, UserRole role)
    {
        // in a real world scenario, we would have password criteria/validation
        // so the following is just a minimal example

        var repo = unitOfWork.UserRepository;
        var existingUserResult = await repo.GetByUsername(username);
        if (existingUserResult.IsUser)
        {
            return new IUserService.DuplicateUsername();
        }

        var hashedPassword = authService.HashPassword(password);
        var newUser = new User
        {
            Role = role,
            Username = username,
            PasswordHash = hashedPassword.PasswordHash,
            PasswordSalt = hashedPassword.Salt,
            ActiveRefreshTokens = []
        };

        repo.Add(newUser);

        return newUser;
    }

    public ValueTask<GetUserResult> GetByUsername(string username) => unitOfWork.UserRepository.GetByUsername(username);
}
