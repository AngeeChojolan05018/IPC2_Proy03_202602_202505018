using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IPC2_Proy03_Frontend.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory fabrica;
        public IndexModel(IHttpClientFactory fabrica) { this.fabrica = fabrica; }

        public int TotalFacturas { get; set; }
        public int FacturasAprobadas { get; set; }
        public int FacturasRechazadas { get; set; }
        public decimal MontoTotalAprobado { get; set; }
        public decimal IvaTotalAprobado { get; set; }
        public decimal MontoTotalRechazado { get; set; }
        public List<FacturaVista> Aprobadas { get; set; } = new();
        public List<FacturaVista> Rechazadas { get; set; } = new();
        public string? ErrorConexion { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var cliente = fabrica.CreateClient("ApiFacturas");
                using var respuesta = await cliente.GetAsync("api/Autorizaciones/estadisticas");
                respuesta.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
                var datos = doc.RootElement;
                TotalFacturas = Entero(datos, "totalFacturas");
                FacturasAprobadas = Entero(datos, "facturasAprobadas");
                FacturasRechazadas = Entero(datos, "facturasRechazadas");
                MontoTotalAprobado = Decimal(datos, "montoTotalAprobado");
                IvaTotalAprobado = Decimal(datos, "ivaTotalAprobado");
                MontoTotalRechazado = Decimal(datos, "montoTotalRechazado");
                Aprobadas = await LeerFacturas(cliente, "api/Autorizaciones/aprobadas");
                Rechazadas = await LeerFacturas(cliente, "api/Autorizaciones/rechazadas");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                ErrorConexion = "No se pudo consultar la API. Comprueba que la API esté ejecutándose en http://localhost:5188 y que los endpoints respondan.";
            }
        }

        private static async Task<List<FacturaVista>> LeerFacturas(HttpClient cliente, string ruta)
        {
            using var respuesta = await cliente.GetAsync(ruta);
            respuesta.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            var resultado = new List<FacturaVista>();
            foreach (var registro in doc.RootElement.EnumerateArray())
            {
                var solicitud = registro.GetProperty("solicitud");
                resultado.Add(new FacturaVista
                {
                    Referencia = Texto(solicitud, "REFERENCIA"),
                    Emisor = Texto(solicitud, "NIT_EMISOR"),
                    Receptor = Texto(solicitud, "NIT_RECEPTOR"),
                    Fecha = Texto(solicitud, "TIEMPO"),
                    Total = Decimal(solicitud, "TOTAL"),
                    Motivo = Texto(registro, "motivoRechazo"),
                    Codigo = TextoNumero(registro, "codigoAprobacion")
                });
            }
            return resultado;
        }

        private static int Entero(JsonElement e, string n) => e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
        private static decimal Decimal(JsonElement e, string n) => e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : 0;
        private static string Texto(JsonElement e, string n) => e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        private static string TextoNumero(JsonElement e, string n) => e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetRawText() : "";

        public class FacturaVista
        {
            public string Referencia { get; set; } = "";
            public string Emisor { get; set; } = "";
            public string Receptor { get; set; } = "";
            public string Fecha { get; set; } = "";
            public decimal Total { get; set; }
            public string Motivo { get; set; } = "";
            public string Codigo { get; set; } = "";
        }
    }
}
