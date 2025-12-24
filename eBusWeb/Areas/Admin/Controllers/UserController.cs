using Microsoft.AspNetCore.Mvc;

namespace eBusWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Create()
        {
            return View();
        }
        public IActionResult Edit()
        {
            return View();
        }
        }
}
