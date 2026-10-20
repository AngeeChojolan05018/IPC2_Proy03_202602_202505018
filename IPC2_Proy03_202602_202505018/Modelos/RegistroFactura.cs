using System.Text.Json.Serialization;

namespace IPC2_Proy03_202602_202505018.Modelos
{
    // Registro persistente de cada factura recibida por el servicio,
    // sea aprobada o rechazada.
    public class RegistroFactura
    {
        [JsonPropertyName("solicitud")]
        public Factura Solicitud { get; set; } = new Factura();

        // Fecha y hora de emisión extraídas de TIEMPO con expresión regular.
        // DateTime.MinValue si no se pudo extraer.
        [JsonPropertyName("fechaEmision")]
        public DateTime FechaEmision { get; set; } = DateTime.MinValue;

        [JsonPropertyName("aprobada")]
        public bool Aprobada { get; set; }

        [JsonPropertyName("codigoAprobacion")]
        public long CodigoAprobacion { get; set; }

        [JsonPropertyName("errores")]
        public ErroresFactura Errores { get; set; } = new ErroresFactura();

        [JsonPropertyName("motivoRechazo")]
        public string MotivoRechazo { get; set; } = string.Empty;
    }
}