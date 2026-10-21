
using System.Text.Json.Serialization;

namespace IPC2_Proy03_202602_202505018.Modelos
{
    public class EstadisticasFacturas
    {
        [JsonPropertyName("totalFacturas")]
        public int TotalFacturas { get; set; }

        [JsonPropertyName("facturasAprobadas")]
        public int FacturasAprobadas { get; set; }

        [JsonPropertyName("facturasRechazadas")]
        public int FacturasRechazadas { get; set; }

        [JsonPropertyName("montoTotalAprobado")]
        public decimal MontoTotalAprobado { get; set; }

        [JsonPropertyName("ivaTotalAprobado")]
        public decimal IvaTotalAprobado { get; set; }

        [JsonPropertyName("montoTotalRechazado")]
        public decimal MontoTotalRechazado { get; set; }
    }
}
