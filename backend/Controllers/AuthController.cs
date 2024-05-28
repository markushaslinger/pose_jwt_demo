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

        return loginResult.Match<ActionResult<TokenResponse>>(success =>
                                                              {
                                                                  var token = success.Value;
                                                                  var response = ToResponse(token);

                                                                  return Ok(response);
                                                              },
                                                              _ => Unauthorized());
    }

    [HttpPost]
    [Route("token-refreshes")]
    public async ValueTask<ActionResult<TokenResponse>> Refresh([FromBody] TokenRefreshRequest request)
    {
        var refreshResult = await _authService.AttemptTokenRefresh(request.Username, request.RefreshToken);

        return refreshResult.Match<ActionResult<TokenResponse>>(success =>
                                                                {
                                                                    var token = success.Value;
                                                                    var response = ToResponse(token);

                                                                    return Ok(response);
                                                                },
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
