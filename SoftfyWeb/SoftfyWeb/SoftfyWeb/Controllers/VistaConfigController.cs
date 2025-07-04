using Microsoft.AspNetCore.Mvc;

namespace SoftfyWeb.Controllers
{
    public class VistaConfigController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
