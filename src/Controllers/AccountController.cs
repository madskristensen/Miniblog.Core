namespace Miniblog.Core.Controllers
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using Miniblog.Core.Models;
    using Miniblog.Core.Services;

    using System.Diagnostics.CodeAnalysis;
    using System.Security.Claims;
    using System.Threading.Tasks;

    /// <summary>
    /// The AccountController class. Implements the <see cref="Microsoft.AspNetCore.Mvc.Controller" />
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// The user services
        /// </summary>
        private readonly IUserServices userServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountController" /> class.
        /// </summary>
        /// <param name="userServices">The user services.</param>
        public AccountController(IUserServices userServices) => this.userServices = userServices;

        /// <summary>
        /// Logins the specified return URL.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        [Route("/login")]
        [AllowAnonymous]
        [HttpGet]
        [SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "MVC binding")]
        public IActionResult Login(string? returnUrl = null)
        {
            this.ViewData[Constants.ReturnUrl] = returnUrl;
            return this.View();
        }

        /// <summary>
        /// login as an asynchronous operation.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <param name="model">The model.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        [Route("/login")]
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        [SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "MVC binding")]
        public async Task<IActionResult> LoginAsync(string? returnUrl, LoginViewModel? model)
        {
            this.ViewData[Constants.ReturnUrl] = returnUrl;

            if (model is null || model.UserName is null || model.Password is null)
            {
                this.ModelState.AddModelError(string.Empty, Properties.Resources.UsernameOrPasswordIsInvalid);
                return this.View(nameof(Login), model);
            }

            if (!this.ModelState.IsValid || !this.userServices.ValidateUser(model.UserName, model.Password))
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

        /// <summary>
        /// log out as an asynchronous operation.
        /// </summary>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        [Route("/logout")]
        public async Task<IActionResult> LogOutAsync()
        {
            await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            return this.LocalRedirect("/");
        }
    }
}
