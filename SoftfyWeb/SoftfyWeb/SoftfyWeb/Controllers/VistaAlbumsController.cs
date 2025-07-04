using Microsoft.AspNetCore.Mvc;

namespace SoftfyWeb.Controllers
{
    public class VistaAlbumsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

