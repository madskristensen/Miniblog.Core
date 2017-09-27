using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Miniblog.Core.Pages
{
    public class LoginModel : PageModel
    {
        private IConfiguration _config;

        public LoginModel(IConfiguration config)
        {
            _config = config;
        }

        public async Task OnGet()
        {
            if (HttpContext.Request.Query.Any(q => q.Key == "logout") && User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                RedirectFromLogin();
            }
        }

        public async Task OnPost(string username, string password, string remember)
        {
            if (username == _config["user:username"] && VerifyHashedPassword(password, _config))
            {
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, _config["user:username"]));

                var principle = new ClaimsPrincipal(identity);
                var properties = new AuthenticationProperties { IsPersistent = remember == "on" };
                await HttpContext.SignInAsync(principle, properties);

                RedirectFromLogin();
            }
        }

        private void RedirectFromLogin()
        {
            if (Request.HasFormContentType &&
                Request.Form.TryGetValue("referrer", out var referrer) &&
                Uri.TryCreate(referrer.ToString(), UriKind.Absolute, out Uri url) &&
                url.Authority == Request.Host.Value)
            {
                HttpContext.Response.Redirect(url.ToString());
            }
            else if (HttpContext.Request.Query.TryGetValue("returnUrl", out var returnUrl))
            {
                HttpContext.Response.Redirect(returnUrl.ToString());
            }
            else
            {
                HttpContext.Response.Redirect("/");
            }
        }

        public static bool VerifyHashedPassword(string password, IConfiguration config)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(config["user:salt"]);

            byte[] hashBytes = KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 1000,
                numBytesRequested: 256 / 8
            );

            string hashText = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            return hashText == config["user:password"];
        }
    }
}
