using JwtDemo.Core;
using JwtDemo.Core.Users;
using JwtDemo.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtDemo.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UserController(IUserService userService, ITransactionProvider transaction) : ControllerBase
{
    #region Insecure Demo Code - Ignore!

    // !! This is obviously just a placeholder, don't let anonymous requests create new users !!
    [HttpPost]
    [AllowAnonymous]
    [Route("registrations")]
    public async ValueTask<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            var addNewResult = await userService.AddNewUser(request.Username, request.Password, request.Role);

            return await addNewResult
                .MatchAsync<IActionResult>(async _ =>
                                           {
                                               await transaction.CommitAsync();

                                               return Created();
                                           },
                                           _ => ValueTask.FromResult<IActionResult>(BadRequest()));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine(ex.Message);

            return Problem();
        }
    }

    #endregion
}
