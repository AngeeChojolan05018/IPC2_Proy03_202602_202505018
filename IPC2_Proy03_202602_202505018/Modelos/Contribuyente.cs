
using System.Text.Json.Serialization;

namespace IPC2_Proy03_202602_202505018.Modelos
{
    public class Contribuyente
    {
        [JsonPropertyName("NIT")]
        public string NIT { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
    }
}

