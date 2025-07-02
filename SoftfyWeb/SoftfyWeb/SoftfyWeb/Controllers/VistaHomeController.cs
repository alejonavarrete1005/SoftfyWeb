using Microsoft.AspNetCore.Mvc;

namespace SoftfyWeb.Controllers
{
    public class VistaHomeController : Controller
    {
        // Acción principal que carga la vista Index
        public IActionResult Index()
        {
            return View();
        }
    }
}
