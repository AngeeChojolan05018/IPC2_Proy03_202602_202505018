
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace IPC2_Proy03_Frontend.Pages
{
    public class ReportesModel : PageModel
    {
        private readonly IHttpClientFactory clienteFactory;

        public ReportesModel(IHttpClientFactory clienteFactory)
        {
            this.clienteFactory = clienteFactory;
        }

        [BindProperty(SupportsGet = true)]
        public string Tipo { get; set; } = "fechas";

        [BindProperty(SupportsGet = true)]
        public DateTime Inicio { get; set; } = new DateTime(2026, 10, 1);

        [BindProperty(SupportsGet = true)]
        public DateTime Fin { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public string Nit { get; set; } = "";

        public string ResultadoJson { get; set; } = "";

        public string Mensaje { get; set; } = "";

        public bool ConsultaRealizada { get; set; }

        public async Task OnGetAsync()
        {
            if (!Request.Query.ContainsKey("Tipo"))
                return;

            ConsultaRealizada = true;

            if (Inicio > Fin)
            {
                Mensaje = "La fecha inicial no puede ser mayor que la final.";
                return;
            }

            string ruta;

            switch (Tipo)
            {
                case "fechas":
                    ruta = "api/Reportes/fechas";
                    break;

                case "contribuyente":
                    if (string.IsNullOrWhiteSpace(Nit))
                    {
                        Mensaje = "Debes ingresar un NIT.";
                        return;
                    }

                    ruta = "api/Reportes/contribuyente";
                    break;

                case "errores":
                    ruta = "api/Reportes/errores";
                    break;

                default:
                    Mensaje = "El tipo de reporte no es válido.";
                    return;
            }

            string parametros =
                "?inicio=" + Inicio.ToString("yyyy-MM-dd") +
                "&fin=" + Fin.ToString("yyyy-MM-dd");

            if (Tipo == "contribuyente")
            {
                parametros += "&nit=" +
                    Uri.EscapeDataString(Nit.Trim());
            }

            try
            {
                HttpClient cliente =
                    clienteFactory.CreateClient("ApiFacturas");

                HttpResponseMessage respuesta =
                    await cliente.GetAsync(ruta + parametros);

                string contenido =
                    await respuesta.Content.ReadAsStringAsync();

                if (!respuesta.IsSuccessStatusCode)
                {
                    Mensaje = "No se pudo generar el reporte: " + contenido;
                    return;
                }

                using JsonDocument documento =
                    JsonDocument.Parse(contenido);

                ResultadoJson = JsonSerializer.Serialize(
                    documento.RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                Mensaje = "Reporte generado correctamente.";
            }
            catch (Exception ex)
            {
                Mensaje = "Error al consultar la API: " + ex.Message;
            }
        }
    }
}
