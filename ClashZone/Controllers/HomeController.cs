using Microsoft.AspNetCore.Mvc;

namespace ClashZone.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
