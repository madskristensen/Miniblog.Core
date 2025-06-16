namespace Miniblog.Core.Controllers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Miniblog.Core.Models;
using Miniblog.Core.Services;

using System.Security.Claims;
using System.Threading.Tasks;

[Authorize]
public class AccountController(IUserServices userServices) : Controller
{
    [Route("/login")]
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        this.ViewData[Constants.ReturnUrl] = returnUrl;
        return this.View();
    }

    [Route("/login")]
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAsync(string? returnUrl, LoginViewModel? model)
    {
        this.ViewData[Constants.ReturnUrl] = returnUrl;

        if (model is null || model.UserName is null || model.Password is null)
        {
            this.ModelState.AddModelError(string.Empty, Properties.Resources.UsernameOrPasswordIsInvalid);
            return this.View(nameof(Login), model);
        }

        if (!this.ModelState.IsValid || !userServices.ValidateUser(model.UserName, model.Password))
        {
            this.ModelState.AddModelError(string.Empty, Properties.Resources.UsernameOrPasswordIsInvalid);
            return this.View(nameof(Login), model);
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, model.UserName));

        var principle = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties { IsPersistent = model.RememberMe };
        await this.HttpContext.SignInAsync(principle, properties).ConfigureAwait(false);

        return this.LocalRedirect(returnUrl ?? "/");
    }

    [Route("/logout")]
    public async Task<IActionResult> LogOutAsync()
    {
        await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        return this.LocalRedirect("/");
    }
}
