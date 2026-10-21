
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Text.Json;

namespace IPC2_Proy03_Frontend.Pages
{
    public class ConsultasModel : PageModel
    {
        private readonly IHttpClientFactory clienteFactory;

        public ConsultasModel(IHttpClientFactory clienteFactory)
        {
            this.clienteFactory = clienteFactory;
        }

        [BindProperty]
        public string Referencia { get; set; } = "";

        [BindProperty]
        public string NIT { get; set; } = "";

        [BindProperty]
        public DateTime? Fecha { get; set; }

        public string Resultado { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public bool Exito { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostReferenciaAsync()
        {
            if (string.IsNullOrWhiteSpace(Referencia))
            {
                Mensaje = "Ingresa una referencia.";
                return Page();
            }

            string ruta = "api/Autorizaciones/buscar/" +
                Uri.EscapeDataString(Referencia.Trim());

            await Consultar(ruta);
            return Page();
        }

        public async Task<IActionResult> OnPostNitAsync()
        {
            if (string.IsNullOrWhiteSpace(NIT))
            {
                Mensaje = "Ingresa un NIT.";
                return Page();
            }

            string ruta = "api/Autorizaciones/nit/" +
                Uri.EscapeDataString(NIT.Trim());

            await Consultar(ruta);
            return Page();
        }

        public async Task<IActionResult> OnPostFechaAsync()
        {
            if (!Fecha.HasValue)
            {
                Mensaje = "Selecciona una fecha.";
                return Page();
            }

            string fechaTexto = Fecha.Value.ToString(
                "yyyy-MM-ddTHH:mm:ss",
                CultureInfo.InvariantCulture);

            string ruta = "api/Autorizaciones/fecha?fecha=" +
                Uri.EscapeDataString(fechaTexto);

            await Consultar(ruta);
            return Page();
        }

        public async Task<IActionResult> OnPostAprobadasAsync()
        {
            await Consultar("api/Autorizaciones/aprobadas");
            return Page();
        }

        public async Task<IActionResult> OnPostRechazadasAsync()
        {
            await Consultar("api/Autorizaciones/rechazadas");
            return Page();
        }

        private async Task Consultar(string ruta)
        {
            try
            {
                HttpClient cliente =
                    clienteFactory.CreateClient("ApiFacturas");

                using HttpResponseMessage respuesta =
                    await cliente.GetAsync(ruta);

                string contenido =
                    await respuesta.Content.ReadAsStringAsync();

                Exito = respuesta.IsSuccessStatusCode;

                if (Exito)
                {
                    Mensaje = "Consulta realizada correctamente.";
                    Resultado = FormatearJson(contenido);
                }
                else if (respuesta.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    Mensaje = "No se encontraron resultados.";
                    Resultado = "";
                }
                else
                {
                    Mensaje = "Error en la consulta: " +
                        (int)respuesta.StatusCode;
                    Resultado = contenido;
                }
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo consultar la API: " +
                    ex.Message;
            }
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
