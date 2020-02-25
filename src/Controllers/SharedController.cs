namespace Miniblog.Core.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    public class SharedController : Controller
    {
        public IActionResult Error() => this.View(this.Response.StatusCode);

        /// <summary>
        /// This is for use in wwwroot/serviceworker.js to support offline scenarios
        /// </summary>
        public IActionResult Offline() => this.View();
    }
}
