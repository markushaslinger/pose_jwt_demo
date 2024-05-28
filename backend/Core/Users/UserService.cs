using JwtDemo.Core.Auth;
using UnionGen;

namespace JwtDemo.Core.Users;

public partial interface IUserService
{
    public ValueTask<AddUserResult> AddNewUser(string username, string password, UserRole role);
    public ValueTask<GetUserResult> GetByUsername(string username);

    [Union<User, DuplicateUsername>]
    public readonly partial struct AddUserResult;

    public readonly struct DuplicateUsername;
}

public sealed class UserService(IUnitOfWork unitOfWork, IAuthService authService) : IUserService
{
    private readonly IAuthService _authService = authService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<IUserService.AddUserResult> AddNewUser(string username, string password, UserRole role)
    {
        // in a real world scenario, we would have password criteria/validation
        // so the following is just a minimal example
        
        await _unitOfWork.BeginTransaction();
        try
        {
            var repo = _unitOfWork.UserRepository;
            var existingUserResult = await repo.GetByUsername(username);
            if (existingUserResult.IsUser)
            {
                return new IUserService.DuplicateUsername();
            }

            var hashedPassword = _authService.HashPassword(password);
            var newUser = new User
            {
                Role = role,
                Username = username,
                PasswordHash = hashedPassword.PasswordHash,
                PasswordSalt = hashedPassword.Salt,
                ActiveRefreshTokens = []
            };

            repo.Add(newUser);
            await _unitOfWork.Commit();

            return newUser;
        }
        catch (Exception ex)
        {
            await _unitOfWork.Rollback();

            // no logging configured in this auth demo project
            Console.WriteLine(ex.Message);

            throw;
        }
    }

    public ValueTask<GetUserResult> GetByUsername(string username) => _unitOfWork.UserRepository.GetByUsername(username);
}
