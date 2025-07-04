using Microsoft.AspNetCore.Mvc;

namespace SoftfyWeb.Controllers
{
    public class VistaPlaylistsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult MiPlaylist()
        {
            return View(); 
        }
    }
}
