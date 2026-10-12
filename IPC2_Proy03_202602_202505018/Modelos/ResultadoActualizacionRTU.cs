
using System.Text.Json.Serialization;

namespace IPC2_Proy03_202602_202505018.Modelos
{
    public class ResultadoActualizacionRTU
    {
        [JsonPropertyName("CONTRIBUYENTES_NUEVOS")]
        public int CONTRIBUYENTES_NUEVOS { get; set; }

        [JsonPropertyName("CONTRIBUYENTES_ACTUALIZADOS")]
        public int CONTRIBUYENTES_ACTUALIZADOS { get; set; }

        [JsonPropertyName("NITS_INVALIDOS")]
        public int NITS_INVALIDOS { get; set; }
    }
}
