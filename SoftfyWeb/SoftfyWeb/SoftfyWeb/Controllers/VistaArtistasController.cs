using Microsoft.AspNetCore.Mvc;


namespace SoftfyWeb.Controllers
{
    public class VistaArtistasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
