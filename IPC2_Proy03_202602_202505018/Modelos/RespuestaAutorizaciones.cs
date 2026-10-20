
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IPC2_Proy03_202602_202505018.Modelos
{
    public class RespuestaAutorizaciones
    {
        [JsonPropertyName("resultadoAutorizaciones")]
        public ResultadoAutorizaciones Resultado { get; set; }
            = new ResultadoAutorizaciones();
    }

    public class ResultadoAutorizaciones
    {
        [JsonPropertyName("cantidadAutorizacionesAprobadas")]
        public int CantidadAutorizacionesAprobadas { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitEmisorInexistente")]
        public int CantidadRechazadasPorEmisorInexistente { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitReceptorInexistente")]
        public int CantidadRechazadasPorReceptorInexistente { get; set; }

        [JsonPropertyName("detalleAprobaciones")]
        public List<DetalleAprobacion> DetalleAprobaciones { get; set; }
            = new List<DetalleAprobacion>();
    }

    public class DetalleAprobacion
    {
        [JsonPropertyName("referencia")]
        public string Referencia { get; set; } = string.Empty;

        [JsonPropertyName("codigoAprobacion")]
        public long CodigoAprobacion { get; set; }
    }
}
