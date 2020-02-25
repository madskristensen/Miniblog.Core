namespace Miniblog.Core.Controllers
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using Miniblog.Core.Models;
    using Miniblog.Core.Services;

    using System.Security.Claims;
    using System.Threading.Tasks;

    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserServices userServices;

        public AccountController(IUserServices userServices) => this.userServices = userServices;

        [Route("/login")]
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            this.ViewData["ReturnUrl"] = returnUrl;
            return this.View();
        }

        [Route("/login")]
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAsync(string returnUrl, LoginViewModel model)
        {
            this.ViewData["ReturnUrl"] = returnUrl;

            if (this.ModelState.IsValid && this.userServices.ValidateUser(model?.UserName, model.Password))
            {
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, model.UserName));

                var principle = new ClaimsPrincipal(identity);
                var properties = new AuthenticationProperties { IsPersistent = model.RememberMe };
                await this.HttpContext.SignInAsync(principle, properties).ConfigureAwait(false);

                return this.LocalRedirect(returnUrl ?? "/");
            }

            this.ModelState.AddModelError(string.Empty, "Username or password is invalid.");
            return this.View("Login", model);
        }

        [Route("/logout")]
        public async Task<IActionResult> LogOutAsync()
        {
            await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            return this.LocalRedirect("/");
        }
    }
}
