using JwtDemo.Core;
using JwtDemo.Core.Auth;
using JwtDemo.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JwtDemo.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, ITransactionProvider transaction) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(Setup.RateLimitPolicyName)]
    [Route("logins")]
    public async ValueTask<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            var loginResult = await authService.AttemptLogin(request.Username, request.Password);

            return await loginResult
                .MatchAsync<ActionResult<TokenResponse>>(async success =>
                                                         {
                                                             await transaction.CommitAsync();

                                                             var token = success.Value;
                                                             var response = ToResponse(token);

                                                             return Ok(response);
                                                         },
                                                         // we don't return a 404 in this case, to not leak information about which users might be valid
                                                         _ => ValueTask
                                                             .FromResult<ActionResult<TokenResponse>>(Unauthorized()),
                                                         _ => ValueTask
                                                             .FromResult<ActionResult<TokenResponse>>(Unauthorized()));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            // no logging configured in this auth demo project
            Console.WriteLine(ex.Message);

            return Problem();
        }
    }

    [HttpPost]
    [Route("logouts")]
    public async ValueTask<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            var logoutResult = await authService.AttemptLogout(request.Username, request.RefreshToken);

            return await logoutResult
                .MatchAsync<IActionResult>(async _ =>
                                           {
                                               await transaction.CommitAsync();

                                               return NoContent();
                                           },
                                           // no 400 response here, again to not leak information about which users might be valid
                                           _ => ValueTask.FromResult<IActionResult>(Unauthorized()));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine(ex.Message);

            return Problem();
        }
    }

    [HttpPost]
    [Route("token-refreshes")]
    public async ValueTask<ActionResult<TokenResponse>> Refresh([FromBody] TokenRefreshRequest request)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            var refreshResult = await authService.AttemptTokenRefresh(request.Username, request.RefreshToken);

            return await refreshResult
                .MatchAsync<ActionResult<TokenResponse>>(async success =>
                                                    {
                                                        await transaction.CommitAsync();

                                                        var token = success.Value;
                                                        var response = ToResponse(token);

                                                        return Ok(response);
                                                    },
                                                    // same reason as before, no 404 in this special case
                                                    _ => ValueTask.FromResult<ActionResult<TokenResponse>>(Unauthorized()),
                                                    _ => ValueTask.FromResult<ActionResult<TokenResponse>>(Unauthorized()));
        } catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine(ex.Message);

            return Problem();
        }
    }

    private static TokenResponse ToResponse(ITokenProvider.NewToken token)
    {
        return new TokenResponse
        {
            AccessToken = ToDto(token.AccessToken),
            RefreshToken = ToDto(token.RefreshToken)
        };

        static TokenDataDto ToDto(ITokenProvider.TokenData tokenData) =>
            new()
            {
                Token = tokenData.Token,
                Expiration = tokenData.Expiration
            };
    }
}
