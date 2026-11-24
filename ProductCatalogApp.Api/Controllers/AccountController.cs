using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProductCatalogApp.Api.Dtos;
namespace ProductCatalogApp.Api.Controllers;

[ApiController, Route("api/account")]
public class AccountController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel input)
    {
        var user = new IdentityUser { UserName = input.Username.Trim() };
        var result = await users.CreateAsync(user, input.Password);
        if (!result.Succeeded) return Invalid(result);
        var role = await users.AddToRoleAsync(user, "User");
        if (!role.Succeeded) { await users.DeleteAsync(user); return Invalid(role); }
        return Ok();
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel input)
    {
        var user = await users.FindByNameAsync(input.Username.Trim());
        if (user == null) return Unauthorized();
        var check = await signIn.CheckPasswordSignInAsync(user, input.Password, true);
        if (!check.Succeeded) return Unauthorized();
        return SignIn(await signIn.CreateUserPrincipalAsync(user), IdentityConstants.BearerScheme);
    }

    [HttpGet("me"), Authorize]
    public IActionResult Me() => Ok(new { username = User.Identity!.Name, roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value) });

    private IActionResult Invalid(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return ValidationProblem(ModelState);
    }
}
