
using System.Text.Json.Serialization;

namespace IPC2_Proy03_202602_202505018.Modelos
{
    public class Factura
    {
        [JsonPropertyName("TIEMPO")]
        public string TIEMPO { get; set; } = "";

        [JsonPropertyName("REFERENCIA")]
        public string REFERENCIA { get; set; } = "";

        [JsonPropertyName("NIT_EMISOR")]
        public string NIT_EMISOR { get; set; } = "";

        [JsonPropertyName("NIT_RECEPTOR")]
        public string NIT_RECEPTOR { get; set; } = "";

        [JsonPropertyName("VALOR")]
        public decimal VALOR { get; set; }

        [JsonPropertyName("IVA")]
        public decimal IVA { get; set; }

        [JsonPropertyName("TOTAL")]
        public decimal TOTAL { get; set; }
    }
}
