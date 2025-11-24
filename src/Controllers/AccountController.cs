using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Miniblog.Core.Models;
using Miniblog.Core.Services;

namespace Miniblog.Core.Controllers;

[Authorize]
public class AccountController(IUserServices userServices) : Controller
{
    [Route("/login")]
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData[Constants.ReturnUrl] = returnUrl;
        return View();
    }

    [Route("/login")]
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAsync(string? returnUrl, LoginViewModel? model)
    {
        ViewData[Constants.ReturnUrl] = returnUrl;

        if (model is null || model.UserName is null || model.Password is null)
        {
            ModelState.AddModelError(string.Empty, Properties.Resources.UsernameOrPasswordIsInvalid);
            return View(nameof(Login), model);
        }

        if (!ModelState.IsValid || !userServices.ValidateUser(model.UserName, model.Password))
        {
            ModelState.AddModelError(string.Empty, Properties.Resources.UsernameOrPasswordIsInvalid);
            return View(nameof(Login), model);
        }

        ClaimsIdentity identity = new(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, model.UserName));

        ClaimsPrincipal principle = new(identity);
        AuthenticationProperties properties = new() { IsPersistent = model.RememberMe };
        await HttpContext.SignInAsync(principle, properties).ConfigureAwait(false);

        return LocalRedirect(returnUrl ?? "/");
    }

    [Route("/logout")]
    public async Task<IActionResult> LogOutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        return LocalRedirect("/");
    }
}
