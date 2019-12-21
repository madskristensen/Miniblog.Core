using Microsoft.AspNetCore.Mvc;

namespace src.Controllers
{
	public class HomeController : Controller
	{
		[Route("/")]
		[OutputCache(Profile = "default")]
		public IActionResult Index()
		{
			return View();
		}
	}
}