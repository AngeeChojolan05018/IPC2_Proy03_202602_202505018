
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace IPC2_Proy03_Frontend.Pages
{
    public class FacturasModel : PageModel
    {
        private readonly IHttpClientFactory clienteFactory;

        public FacturasModel(IHttpClientFactory clienteFactory)
        {
            this.clienteFactory = clienteFactory;
        }

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        [BindProperty]
        public string ContenidoJson { get; set; } = "";

        public string Mensaje { get; set; } = "";
        public string Resultado { get; set; } = "";
        public bool Exito { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                string contenido = ContenidoJson ?? "";

                // Si se seleccionó un archivo, tiene prioridad.
                if (Archivo != null && Archivo.Length > 0)
                {
                    if (Archivo.Length > 5 * 1024 * 1024)
                    {
                        Mensaje = "El archivo supera los 5 MB.";
                        return Page();
                    }

                    using StreamReader lector =
                        new StreamReader(Archivo.OpenReadStream());

                    contenido = await lector.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(contenido))
                {
                    Mensaje = "Selecciona un archivo JSON o pega las facturas.";
                    return Page();
                }

                using JsonDocument documento =
                    JsonDocument.Parse(contenido);

                if (documento.RootElement.ValueKind !=
                    JsonValueKind.Array)
                {
                    Mensaje = "Debes enviar un arreglo JSON de facturas.";
                    return Page();
                }

                if (documento.RootElement.GetArrayLength() == 0)
                {
                    Mensaje = "El arreglo de facturas está vacío.";
                    return Page();
                }

                HttpClient cliente =
                    clienteFactory.CreateClient("ApiFacturas");

                using StringContent cuerpo = new StringContent(
                    contenido,
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage respuesta =
                    await cliente.PostAsync(
                        "api/Autorizaciones", cuerpo);

                string texto =
                    await respuesta.Content.ReadAsStringAsync();

                Resultado = FormatearJson(texto);
                Exito = respuesta.IsSuccessStatusCode;

                Mensaje = Exito
                    ? "Solicitud procesada. Revisa las aprobaciones y rechazos."
                    : "La API rechazó la solicitud: " +
                      (int)respuesta.StatusCode;
            }
            catch (JsonException)
            {
                Mensaje = "El contenido no tiene un formato JSON válido.";
            }
            catch (Exception ex)
            {
                Mensaje = "Error de conexión o procesamiento: " +
                    ex.Message;
            }

            return Page();
        }

        private static string FormatearJson(string contenido)
        {
            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(contenido);

                return JsonSerializer.Serialize(
                    documento.RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
            }
            catch
            {
                return contenido;
            }
        }
    }
}
