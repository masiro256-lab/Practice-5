using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCatalogApp.Client.Models;
namespace ProductCatalogApp.Client.Controllers;

public class AccountController(IHttpClientFactory clients) : Controller
{
    private record TokenResponse(string AccessToken, int ExpiresIn);
    private record UserResponse(string Username, string[] Roles);

    [AllowAnonymous] public IActionResult Register() => View();
    [AllowAnonymous] public IActionResult Login() => View();

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        using var api = clients.CreateClient("CatalogApi");
        using var response = await api.PostAsJsonAsync("api/account/register", model);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            foreach (var messages in problem?.Errors.Values ?? []) foreach (var message in messages) ModelState.AddModelError("", message);
            return View(model);
        }
        return await Authenticate(model.Username, model.Password, false, null) ? RedirectToAction("Index", "Home") : RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);
        if (await Authenticate(model.Username, model.Password, model.RememberMe, returnUrl))
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
        ModelState.AddModelError("", "Неверные данные или вход временно заблокирован.");
        return View(model);
    }

    private async Task<bool> Authenticate(string username, string password, bool remember, string? returnUrl)
    {
        using var api = clients.CreateClient("CatalogApi");
        using var response = await api.PostAsJsonAsync("api/account/login", new { username, password });
        if (!response.IsSuccessStatusCode) return false;
        var token = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var user = (await api.GetFromJsonAsync<UserResponse>("api/account/me"))!;
        var claims = new List<Claim> { new(ClaimTypes.Name, user.Username) };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var properties = new AuthenticationProperties { IsPersistent = remember, ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn) };
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = token.AccessToken }]);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), properties);
        return true;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [AllowAnonymous]
    public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }
}
