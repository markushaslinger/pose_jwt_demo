using JwtDemo.Core.Auth;
using JwtDemo.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtDemo.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost]
    [Route("logins")]
    public async ValueTask<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        var loginResult = await _authService.AttemptLogin(request.Username, request.Password);

        return loginResult
            .Match<ActionResult<TokenResponse>>(success =>
                                                {
                                                    var token = success.Value;
                                                    var response = ToResponse(token);

                                                    return Ok(response);
                                                },
                                                // we don't return a 404 in this case, to not leak information about which users might be valid
                                                _ => Unauthorized(),
                                                _ => Unauthorized());
    }

    [HttpPost]
    [Route("logouts")]
    public async ValueTask<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var logoutResult = await _authService.AttemptLogout(request.Username, request.RefreshToken);

        return logoutResult
            .Match<IActionResult>(_ => NoContent(),
                                  // no 400 response here, again to not leak information about which users might be valid
                                  _ => Unauthorized());
    }

    [HttpPost]
    [Route("token-refreshes")]
    public async ValueTask<ActionResult<TokenResponse>> Refresh([FromBody] TokenRefreshRequest request)
    {
        var refreshResult = await _authService.AttemptTokenRefresh(request.Username, request.RefreshToken);

        return refreshResult
            .Match<ActionResult<TokenResponse>>(success =>
                                                {
                                                    var token = success.Value;
                                                    var response = ToResponse(token);

                                                    return Ok(response);
                                                },
                                                // same reason as before, no 404 in this special case
                                                _ => Unauthorized(),
                                                _ => Unauthorized());
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
