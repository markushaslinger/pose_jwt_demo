using JwtDemo.Core.Users;
using JwtDemo.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtDemo.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    #region Insecure Demo Code - Ignore!

    // !! This is obviously just a placeholder, don't let anonymous requests create new users !!
    [HttpPost]
    [AllowAnonymous]
    [Route("registrations")]
    public async ValueTask<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var addNewResult = await _userService.AddNewUser(request.Username, request.Password, request.Role);

        return addNewResult.Match<IActionResult>(_ => Created(),
                                                 _ => BadRequest());
    }

    #endregion

}
