
using System.Globalization;
using System.Text.Json;
using IPC2_Proy03_202602_202505018.Modelos;

namespace IPC2_Proy03_202602_202505018.Servicios
{
    public class AutorizacionService
    {
        private readonly string rutaArchivo;
        private readonly RTUService servicioRTU;
        private readonly ValidadorNIT validador;

        public AutorizacionService(
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment entorno,
            RTUService servicioRTU,
            ValidadorNIT validador)
        {
            rutaArchivo = Path.Combine(
                entorno.ContentRootPath,
                "Almacenamiento",
                "facturas.json");

            this.servicioRTU = servicioRTU;
            this.validador = validador;

            string? carpeta = Path.GetDirectoryName(rutaArchivo);

            if (carpeta != null)
            {
                Directory.CreateDirectory(carpeta);
            }

            if (!File.Exists(rutaArchivo))
            {
                File.WriteAllText(rutaArchivo, "[]");
            }
        }

        public RespuestaAutorizaciones Procesar(
            List<Factura> facturas)
        {
            List<RegistroFactura> registros = ObtenerRegistros();
            List<Contribuyente> contribuyentes =
                servicioRTU.ObtenerContribuyentes();

            RespuestaAutorizaciones respuesta =
                new RespuestaAutorizaciones();

            string fechaActual = DateTime.Now.ToString("yyyyMMdd");

            foreach (Factura factura in facturas)
            {
                RegistroFactura registro = new RegistroFactura
                {
                    Solicitud = factura
                };

                string tiempo = factura.TIEMPO ?? string.Empty;

                string fechaTexto = tiempo
                    .Replace("Guatemala,", "")
                    .Replace("hrs.", "")
                    .Trim();

                if (DateTime.TryParseExact(
                    fechaTexto,
                    "dd/MM/yyyy HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fechaEmision))
                {
                    registro.FechaEmision = fechaEmision;
                }
                else
                {
                    registro.Errores.FechaInvalida = true;

                    registro.MotivoRechazo =
                        "La fecha de emisión es inválida.";

                    registros.Add(registro);
                    continue;
                }

                string referencia =
                    (factura.REFERENCIA ?? string.Empty).Trim();

                factura.REFERENCIA = referencia;

                if (referencia.Length == 0 || referencia.Length > 40)
                {
                    registro.MotivoRechazo =
                        "La referencia debe tener entre 1 y 40 caracteres.";

                    registros.Add(registro);
                    continue;
                }

                bool referenciaDuplicada = registros.Exists(
                    r => string.Equals(
                        r.Solicitud.REFERENCIA.Trim(),
                        referencia,
                        StringComparison.OrdinalIgnoreCase));

                if (referenciaDuplicada)
                {
                    registro.MotivoRechazo =
                        "La referencia ya fue utilizada.";

                    registros.Add(registro);
                    continue;
                }

                string nitEmisor = NormalizarNit(factura.NIT_EMISOR);
                string nitReceptor = NormalizarNit(factura.NIT_RECEPTOR);

                bool emisorValido = validador.EsValido(nitEmisor);
                bool receptorValido = validador.EsValido(nitReceptor);

                if (!emisorValido || !receptorValido)
                {
                    registro.MotivoRechazo = "NIT inválido.";

                    registros.Add(registro);
                    continue;
                }

                bool emisorExiste = contribuyentes.Exists(
                    c => NormalizarNit(c.NIT) == nitEmisor);

                bool receptorExiste = contribuyentes.Exists(
                    c => NormalizarNit(c.NIT) == nitReceptor);

                if (!emisorExiste || !receptorExiste)
                {
                    if (!emisorExiste)
                    {
                        respuesta.Resultado
                            .CantidadRechazadasPorEmisorInexistente++;
                    }

                    if (!receptorExiste)
                    {
                        respuesta.Resultado
                            .CantidadRechazadasPorReceptorInexistente++;
                    }

                    registro.MotivoRechazo =
                        "El emisor o el receptor no existe en el RTU.";

                    registros.Add(registro);
                    continue;
                }

                decimal ivaEsperado = decimal.Round(
                    factura.VALOR * 0.12m,
                    2,
                    MidpointRounding.AwayFromZero);

                decimal totalEsperado =
                    factura.VALOR + ivaEsperado;

                if (factura.VALOR <= 0 ||
                    factura.IVA != ivaEsperado ||
                    factura.TOTAL != totalEsperado)
                {
                    registro.MotivoRechazo =
                        "El valor, el IVA o el total son incorrectos.";

                    registros.Add(registro);
                    continue;
                }

                int correlativo = ObtenerSiguienteCorrelativo(
                    registros,
                    fechaActual);

                string codigoTexto = fechaActual +
                    correlativo.ToString("D8");

                long codigo = long.Parse(
                    codigoTexto,
                    CultureInfo.InvariantCulture);

                registro.Aprobada = true;
                registro.CodigoAprobacion = codigo;

                registros.Add(registro);

                respuesta.Resultado.CantidadAutorizacionesAprobadas++;

                respuesta.Resultado.DetalleAprobaciones.Add(
                    new DetalleAprobacion
                    {
                        Referencia = referencia,
                        CodigoAprobacion = codigo
                    });
            }

            GuardarRegistros(registros);

            return respuesta;
        }

        public List<RegistroFactura> ObtenerFacturas()
        {
            return ObtenerRegistros();
        }
        private List<RegistroFactura> ObtenerRegistros()
        {
            string contenido = File.ReadAllText(rutaArchivo);

            return JsonSerializer.Deserialize<List<RegistroFactura>>(
                contenido) ?? new List<RegistroFactura>();
        }

        private void GuardarRegistros(
            List<RegistroFactura> registros)
        {
            string json = JsonSerializer.Serialize(
                registros,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(rutaArchivo, json);
        }

        private static string NormalizarNit(string? nit)
        {
            return (nit ?? string.Empty)
                .Trim()
                .Replace("-", "")
                .ToUpperInvariant();
        }

        private static int ObtenerSiguienteCorrelativo(
            List<RegistroFactura> registros,
            string fechaActual)
        {
            int mayor = 0;

            foreach (RegistroFactura registro in registros)
            {
                if (!registro.Aprobada)
                {
                    continue;
                }

                string codigo = registro.CodigoAprobacion.ToString(
                    CultureInfo.InvariantCulture);

                if (codigo.Length != 16 ||
                    !codigo.StartsWith(fechaActual))
                {
                    continue;
                }

                if (int.TryParse(
                    codigo.Substring(8),
                    out int correlativo) &&
                    correlativo > mayor)
                {
                    mayor = correlativo;
                }
            }

            return mayor + 1;
        }
    }
}
