using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos;
using SoftfyWeb.Modelos.Dtos;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SoftfyWeb.Controllers
{
    public class VistasAuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VistasAuthController(IHttpClientFactory httpClientFactory)
            => _httpClientFactory = httpClientFactory;

        // Helper para inyectar el token JWT desde la cookie
        private HttpClient ObtenerClienteConToken()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var jwt = User.FindFirst("jwt")?.Value;
            if (!string.IsNullOrEmpty(jwt))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
            return client;
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!IsValidEmail(dto.Email))
                ModelState.AddModelError(nameof(dto.Email), "Correo inválido.");
            if (!ModelState.IsValid)
                return View(dto);

            var client = _httpClientFactory.CreateClient();
            var resp = await client.PostAsync(
                "https://localhost:7003/api/auth/forgot-password",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
            );
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", raw);
                return View(dto);
            }
            TempData["Info"] = "Si el correo existe, recibirás instrucciones para restablecer tu contraseña.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token, string email)
            => View(new ResetPasswordDto { Email = email, Token = token });

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                ModelState.AddModelError(nameof(dto.NewPassword), "La contraseña debe tener al menos 6 caracteres.");
            if (!ModelState.IsValid)
                return View(dto);

            var client = _httpClientFactory.CreateClient();
            var resp = await client.PostAsync(
                "https://localhost:7003/api/auth/reset-password",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
            );
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", raw);
                return View(dto);
            }
            TempData["Info"] = "Contraseña restablecida correctamente. Ahora puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Cierra la sesión en la cookie 'auth_cookie'
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult RegistroArtista() => View();

        [HttpGet]
        public IActionResult Registro() => View(new UsuarioRegistroDto());

        [HttpPost]
        public async Task<IActionResult> Registro(UsuarioRegistroDto dto)
        {
            if (!EsContrasenaSegura(dto.Password))
                ModelState.AddModelError(nameof(dto.Password), "Debe tener ≥6 caracteres, 1 mayúscula y 1 número.");
            if (!IsValidEmail(dto.Email))
                ModelState.AddModelError(nameof(dto.Email), "Correo inválido.");
            if (!ModelState.IsValid)
                return View(dto);

            var client = _httpClientFactory.CreateClient();
            var url = dto.TipoUsuario == "Artista"
                ? "https://localhost:7003/api/auth/registro-artista"
                : "https://localhost:7003/api/auth/registro";
            var resp = await client.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
            );
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", raw);
                return View(dto);
            }
            TempData["RegistroOk"] = "¡Registro exitoso! Revisa tu correo y luego inicia sesión.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home"); 

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Info = TempData["RegistroOk"];
            return View(new UsuarioLoginDto());
        }

        [HttpPost]
        public async Task<IActionResult> Login(UsuarioLoginDto dto, string returnUrl = null)
        {
            var client = _httpClientFactory.CreateClient();
            var resp = await client.PostAsJsonAsync("https://localhost:7003/api/auth/login", dto);
            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = "Credenciales inválidas.";
                return View(dto);
            }
            var token = JsonDocument.Parse(raw)
                                   .RootElement
                                   .GetProperty("token")
                                   .GetString();

            // 1) Parseamos el JWT y extraemos sus claims
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // 2) Construimos la identidad con esquema de cookies
            var identity = new ClaimsIdentity(
                jwtToken.Claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            // Guardamos el raw JWT como claim para usarlo luego en HttpClient
            identity.AddClaim(new Claim("jwt", token));

            var principal = new ClaimsPrincipal(identity);

            // 3) Firmamos al usuario: el middleware creará/actualizará 'auth_cookie'
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2),
                    IsPersistent = false
                }
            );
            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            if (role == "Artista")
                return RedirectToAction(nameof(BienvenidoArtista));
            if (role == "Oyente")
                return RedirectToAction(nameof(BienvenidoOyente));

            return RedirectToAction(nameof(Bienvenido));
        }


        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var client = _httpClientFactory.CreateClient();
            var resp = await client.GetAsync(
                $"https://localhost:7003/api/auth/confirmar-email?userId={userId}&token={token}"
            );
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "Hubo un error al confirmar tu correo.";
                return RedirectToAction(nameof(Login));
            }
            TempData["Info"] = "Tu correo ha sido confirmado correctamente. Ahora puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize(Roles = "Artista")]
        public async Task<IActionResult> BienvenidoArtista()
        {
            var nombreArtistico = User.Identity.Name;
            try
            {
                var client = ObtenerClienteConToken();
                var resp = await client.GetAsync("artistas/mi-perfil");
                if (resp.IsSuccessStatusCode)
                {
                    var raw = await resp.Content.ReadAsStringAsync();
                    var perfil = JsonSerializer.Deserialize<PerfilArtistaDto>(raw,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (perfil != null)
                        nombreArtistico = perfil.NombreArtistico;
                }
            }
            catch
            {
                // opcional: loguear el error
            }
            ViewBag.ArtistaNombre = nombreArtistico;
            return View();
        }

        public async Task<IActionResult> BienvenidoOyente()
        {
            var nombreOyente = User.Identity.Name;
            try
            {
                var client = ObtenerClienteConToken();
                var resp = await client.GetAsync("oyentes/mi-perfil");
                if (resp.IsSuccessStatusCode)
                {
                    var raw = await resp.Content.ReadAsStringAsync();
                    var perfil = JsonSerializer.Deserialize<PerfilOyenteDto>(raw,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (perfil != null)
                    {
                        nombreOyente = $"{perfil.Nombre} {perfil.Apellido}";
                    }
                }
            }
            catch
            {
            }
            ViewBag.OyenteNombre = nombreOyente;
            // Obtener todas las canciones del sistema desde la API usando el endpoint proporcionado
            var clientCanciones = ObtenerClienteConToken();
            var respCanciones = await clientCanciones.GetAsync("https://localhost:7003/api/Canciones/canciones"); // Endpoint correcto
            var todasCanciones = new List<CancionRespuestaDto>();
            if (respCanciones.IsSuccessStatusCode)
            {
                var rawCanciones = await respCanciones.Content.ReadAsStringAsync();
                todasCanciones = JsonSerializer.Deserialize<List<CancionRespuestaDto>>(rawCanciones,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Asegurarse de que la URL del archivo esté correctamente formada
                foreach (var cancion in todasCanciones)
                {
                    var nombreArchivo = Path.GetFileName(cancion.UrlArchivo);
                    cancion.UrlArchivo = $"https://localhost:7003/api/canciones/reproducir/{nombreArchivo}";
                }
            }

            // Pasar las canciones al ViewBag
            ViewBag.TodasCanciones = todasCanciones;

            return View();
        }



        public IActionResult Bienvenido() => View();

        // Métodos auxiliares
        private bool EsContrasenaSegura(string pwd) =>
            !string.IsNullOrEmpty(pwd)
            && pwd.Length >= 6
            && pwd.Any(char.IsUpper)
            && pwd.Any(char.IsDigit);

        private bool IsValidEmail(string em)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(em);
                return addr.Address == em;
            }
            catch { return false; }
        }
    }
}