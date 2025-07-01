﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Modelos.Dtos;
using SoftfyWeb.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace SoftfyWeb.Controllers
{
    [Authorize]
    public class VistasPlaylistsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VistasPlaylistsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient ObtenerClienteConToken()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private ErrorViewModel CrearErrorModel()
        {
            string id = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return new ErrorViewModel { RequestId = id };
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        public async Task<IActionResult> Index()
        {
            var client = ObtenerClienteConToken();
            var resp = await client.GetAsync("playlists/mis-playlists");
            if (!resp.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var raw = await resp.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var listas = JsonSerializer.Deserialize<List<PlaylistDto>>(raw, opts);
            return View(listas);
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost, Authorize(Roles = "OyentePremium,Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError("", "El nombre es requerido");
                return View();
            }

            var client = ObtenerClienteConToken();
            var resp = await client.PostAsJsonAsync("playlists/crear", nombre);
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var err = await resp.Content.ReadAsStringAsync();
            ModelState.AddModelError("", err);
            return View();
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        public async Task<IActionResult> Detalle(int id)
        {
            var client = ObtenerClienteConToken();

            // 1️⃣ Obtener las canciones que ya están en la playlist
            var resp = await client.GetAsync($"playlists/{id}/canciones");
            if (!resp.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var raw = await resp.Content.ReadAsStringAsync();
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var cancionesPlaylist = JsonSerializer.Deserialize<List<PlaylistCancionDto>>(raw, opciones);

            // Ajustar URL de reproducción
            foreach (var c in cancionesPlaylist)
            {
                var nombreArchivo = Path.GetFileName(c.UrlArchivo);
                c.UrlArchivo = $"https://localhost:7003/api/canciones/reproducir/{nombreArchivo}";
            }

            // 2️⃣ Obtener **todas** las canciones del artista actual
            var respArtista = await client.GetAsync("canciones/mis-canciones");
            List<CancionDto> cancionesArtista = new();
            if (respArtista.IsSuccessStatusCode)
            {
                var rawArtista = await respArtista.Content.ReadAsStringAsync();
                cancionesArtista = JsonSerializer.Deserialize<List<CancionDto>>(rawArtista, opciones);
            }

            ViewBag.PlaylistId = id;
            ViewBag.CancionesArtista = cancionesArtista;

            return View(cancionesPlaylist);
        }



        [HttpPost, Authorize(Roles = "OyentePremium,Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Renombrar(int id, string nuevoNombre)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.PutAsJsonAsync($"playlists/{id}/renombrar", nuevoNombre);
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var err = await resp.Content.ReadAsStringAsync();
            TempData["Error"] = err;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, Authorize(Roles = "OyentePremium,Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.DeleteAsync($"playlists/{id}/eliminar");
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            return View("Error", CrearErrorModel());
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> QuitarCancion(int playlistId, int cancionId)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.DeleteAsync($"playlists/{playlistId}/quitar/{cancionId}");
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Detalle), new { id = playlistId });

            return View("Error", CrearErrorModel());
        }

        [HttpPost, Authorize(Roles = "OyentePremium,Artista"), ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarCancion(int playlistId, int cancionId)
        {
            var client = ObtenerClienteConToken();

            // Llamamos al endpoint de la API que añade la canción a la playlist
            var resp = await client.PostAsync($"playlists/{playlistId}/agregar/{cancionId}", null);

            if (resp.IsSuccessStatusCode)
            {
                // Redirigimos al detalle para recargar la lista
                return RedirectToAction(nameof(Detalle), new { id = playlistId });
            }

            TempData["Error"] = "No se pudo agregar la canción. Intenta de nuevo.";
            return RedirectToAction(nameof(Detalle), new { id = playlistId });
        }




        [HttpPost, Authorize]
        public async Task<IActionResult> QuitarMeGusta(int cancionId)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.DeleteAsync($"playlists/me-gusta/{cancionId}");
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(MeGusta));

            return View("Error", CrearErrorModel());
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> DarMeGusta(int cancionId)
        {
            var client = ObtenerClienteConToken();
            var resp = await client.PostAsync($"playlists/me-gusta/{cancionId}", null);
            if (resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(MeGusta));

            return View("Error", CrearErrorModel());
        }

        [Authorize(Roles = "OyentePremium,Artista")]
        public async Task<IActionResult> MeGusta()
        {
            var client = ObtenerClienteConToken();
            var resp = await client.GetAsync("playlists/me-gusta");

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                // Nada en Me Gusta: devolvemos la vista con lista vacía y mensaje
                ViewBag.Message = "No tienes canciones marcadas como Me Gusta.";
                var emptyDto = new MeGustaRespuestaDto
                {
                    Nombre = null,
                    Total = 0,
                    Canciones = null
                };
                return View(emptyDto);
            }

            if (!resp.IsSuccessStatusCode)
            {
                // Otros errores
                return View("Error", CrearErrorModel());
            }

            var raw = await resp.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = JsonSerializer.Deserialize<MeGustaRespuestaDto>(raw, opts);

            return View(dto);
        }
    }
}