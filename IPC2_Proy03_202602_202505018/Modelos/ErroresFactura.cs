using System.Text.Json.Serialization;

namespace IPC2_Proy03_202602_202505018.Modelos
{
    public class ErroresFactura
    {
        [JsonPropertyName("nitEmisorInvalido")]
        public bool NitEmisorInvalido { get; set; }

        [JsonPropertyName("nitReceptorInvalido")]
        public bool NitReceptorInvalido { get; set; }

        [JsonPropertyName("nitEmisorInexistente")]
        public bool NitEmisorInexistente { get; set; }

        [JsonPropertyName("nitReceptorInexistente")]
        public bool NitReceptorInexistente { get; set; }

        [JsonPropertyName("ivaMalCalculado")]
        public bool IvaMalCalculado { get; set; }

        [JsonPropertyName("totalMalCalculado")]
        public bool TotalMalCalculado { get; set; }

        [JsonPropertyName("referenciaDuplicada")]
        public bool ReferenciaDuplicada { get; set; }

        [JsonPropertyName("referenciaInvalida")]
        public bool ReferenciaInvalida { get; set; }

        [JsonPropertyName("fechaInvalida")]
        public bool FechaInvalida { get; set; }

        [JsonPropertyName("valorInvalido")]
        public bool ValorInvalido { get; set; }

        [JsonIgnore]
        public bool TieneErrores =>
            NitEmisorInvalido ||
            NitReceptorInvalido ||
            NitEmisorInexistente ||
            NitReceptorInexistente ||
            IvaMalCalculado ||
            TotalMalCalculado ||
            ReferenciaDuplicada ||
            ReferenciaInvalida ||
            FechaInvalida ||
            ValorInvalido;
    }
}