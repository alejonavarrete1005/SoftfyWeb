using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SoftfyWeb.Dtos;
using SoftfyWeb.Modelos.Dtos;
using SoftfyWeb.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SoftfyWeb.Controllers
{
    [Authorize]
    public class VistasSuscripcionesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VistasSuscripcionesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient ObtenerClienteConToken()
        {
            var client = _httpClientFactory.CreateClient("SoftfyApi");
            var token = Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization
                      = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private HttpClient ObtenerCliente()
        {
            return _httpClientFactory.CreateClient("SoftfyApi");
        }

        private ErrorViewModel CrearErrorModel()
        {
            string id = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return new ErrorViewModel { RequestId = id };
        }
        public async Task<IActionResult> Estado()
        {
            HttpClient client = ObtenerClienteConToken();
            HttpResponseMessage response = await client.GetAsync("suscripciones/estado"); // Solicita el estado de la suscripción
            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var json = await response.Content.ReadAsStringAsync();
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var estadoSuscripcion = JsonSerializer.Deserialize<SuscripcionEstadoDto>(json, opciones);

            return View(estadoSuscripcion);
        }

        [HttpGet]
        public async Task<IActionResult> ActivarSuscripcion()
        {
            // Obtener los planes desde la API
            HttpClient client = ObtenerClienteConToken();
            HttpResponseMessage response = await client.GetAsync("https://localhost:7003/api/Planes");
            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine(json); // Para depuración, ver el JSON recibido
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var planes = JsonSerializer.Deserialize<List<PlanDto>>(json, opciones);

            // Asegurarse de que los datos se deserializan correctamente
            if (planes == null || !planes.Any())
            {
                return View("Error", CrearErrorModel());  // Si no hay datos, muestra el error
            }
            return View(planes);
        }

        [HttpPost]
        public async Task<IActionResult> ActivarSuscripcion(int planId)
        {
            Console.WriteLine("Plan seleccionado: " + planId);  // Para verificar que se recibe correctamente

            var client = ObtenerClienteConToken();

            // Crear el contenido de la solicitud con el valor de planId como un simple valor entero (texto plano)
            var content = new StringContent(planId.ToString(), Encoding.UTF8, "text/plain");

            // Enviar la solicitud POST con el planId
            var response = await client.PostAsync("suscripciones/activar", content);

            // Verificar el estado de la respuesta
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error al activar la suscripción. Código de estado: " + response.StatusCode);
                return View("Error", CrearErrorModel());
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RespuestaDto>(json);

            // Verificar el resultado
            Console.WriteLine("Resultado de la activación: " + json);

            return RedirectToAction("Estado");
        }




        // 3) Agregar miembro a la suscripción (solo para el titular de la suscripción)
        [HttpPost]
        public async Task<IActionResult> AgregarMiembro(string email)
        {
            var client = ObtenerClienteConToken();
            var response = await client.PostAsync("suscripciones/agregar-miembro", new StringContent(email));
            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RespuestaDto>(json);

            // Redirigir al estado de la suscripción después de agregar el miembro
            return RedirectToAction("Estado");
        }

        // 4) Eliminar miembro de la suscripción (solo para el titular de la suscripción)
        [HttpPost]
        public async Task<IActionResult> EliminarMiembro(string email)
        {
            var client = ObtenerClienteConToken();
            var response = await client.PostAsync("suscripciones/eliminar-miembro", new StringContent(email));
            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RespuestaDto>(json);

            // Redirigir al estado de la suscripción después de eliminar el miembro
            return RedirectToAction("Estado");
        }

        // 5) Salir de la suscripción (solo para miembros premium)
        [HttpPost]
        public async Task<IActionResult> SalirDeSuscripcion()
        {
            var client = ObtenerClienteConToken();
            var response = await client.PostAsync("suscripciones/salir-de-suscripcion", new StringContent(""));

            if (!response.IsSuccessStatusCode)
                return View("Error", CrearErrorModel());

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RespuestaDto>(json);

            // Redirigir a la vista de estado después de salir de la suscripción
            return RedirectToAction("Estado");
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(CrearErrorModel());
        }
    }
}
