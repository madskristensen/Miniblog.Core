namespace Miniblog.Core.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// The SharedController class. Implements the <see cref="Microsoft.AspNetCore.Mvc.Controller" />
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    public class SharedController : Controller
    {
        /// <summary>
        /// Returns the view for errors.
        /// </summary>
        /// <returns>IActionResult.</returns>
        public IActionResult Error() => this.View(this.Response.StatusCode);

        /// <summary>
        /// This is for use in wwwroot/serviceworker.js to support offline scenarios
        /// </summary>
        /// <returns>IActionResult.</returns>
        public IActionResult Offline() => this.View();
    }
}
