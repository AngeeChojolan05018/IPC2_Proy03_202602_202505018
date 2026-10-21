
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

                // VALIDAR FECHA

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

                // VALIDAR REFERENCIA

                string referencia =
                    (factura.REFERENCIA ?? string.Empty).Trim();

                factura.REFERENCIA = referencia;

                if (referencia.Length == 0 ||
                    referencia.Length > 40)
                {
                    registro.Errores.ReferenciaInvalida = true;

                    registro.MotivoRechazo =
                        "La referencia debe tener entre 1 y 40 caracteres.";

                    registros.Add(registro);
                    continue;
                }

                bool referenciaDuplicada = registros.Exists(
                    r => string.Equals(
                        r.Solicitud.REFERENCIA,
                        referencia,
                        StringComparison.OrdinalIgnoreCase));

                if (referenciaDuplicada)
                {
                    registro.Errores.ReferenciaDuplicada = true;

                    registro.MotivoRechazo =
                        "La referencia ya fue utilizada.";

                    registros.Add(registro);
                    continue;
                }

                // VALIDAR NIT

                string nitEmisor =
                    NormalizarNit(factura.NIT_EMISOR);

                string nitReceptor =
                    NormalizarNit(factura.NIT_RECEPTOR);

                bool emisorValido =
                    validador.EsValido(nitEmisor);

                bool receptorValido =
                    validador.EsValido(nitReceptor);

                if (!emisorValido || !receptorValido)
                {
                    registro.Errores.NitEmisorInvalido =
                        !emisorValido;

                    registro.Errores.NitReceptorInvalido =
                        !receptorValido;

                    registro.MotivoRechazo = "NIT inválido.";

                    registros.Add(registro);
                    continue;
                }

                // COMPROBAR EXISTENCIA EN RTU

                bool emisorExiste = contribuyentes.Exists(
                    c => NormalizarNit(c.NIT) == nitEmisor);

                bool receptorExiste = contribuyentes.Exists(
                    c => NormalizarNit(c.NIT) == nitReceptor);

                if (!emisorExiste || !receptorExiste)
                {
                    if (!emisorExiste)
                    {
                        registro.Errores.NitEmisorInexistente = true;

                        respuesta.Resultado
                            .CantidadRechazadasPorEmisorInexistente++;
                    }

                    if (!receptorExiste)
                    {
                        registro.Errores.NitReceptorInexistente = true;

                        respuesta.Resultado
                            .CantidadRechazadasPorReceptorInexistente++;
                    }

                    registro.MotivoRechazo =
                        "El emisor o el receptor no existe en el RTU.";

                    registros.Add(registro);
                    continue;
                }

                // VALIDAR VALOR

                if (factura.VALOR <= 0)
                {
                    registro.Errores.ValorInvalido = true;

                    registro.MotivoRechazo =
                        "El valor de la factura debe ser mayor que cero.";

                    registros.Add(registro);
                    continue;
                }

                // VALIDAR IVA Y TOTAL

                decimal ivaEsperado = decimal.Round(
                    factura.VALOR * 0.12m,
                    2,
                    MidpointRounding.AwayFromZero);

                decimal totalEsperado =
                    factura.VALOR + ivaEsperado;

                bool ivaIncorrecto =
                    factura.IVA != ivaEsperado;

                bool totalIncorrecto =
                    factura.TOTAL != totalEsperado;

                if (ivaIncorrecto || totalIncorrecto)
                {
                    registro.Errores.IvaMalCalculado =
                        ivaIncorrecto;

                    registro.Errores.TotalMalCalculado =
                        totalIncorrecto;

                    registro.MotivoRechazo =
                        "El IVA o el total son incorrectos.";

                    registros.Add(registro);
                    continue;
                }

                // GENERAR CODIGO DE APROBACION

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

                respuesta.Resultado
                    .CantidadAutorizacionesAprobadas++;

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


        public EstadisticasFacturas ObtenerEstadisticas()
        {
            List<RegistroFactura> registros = ObtenerRegistros();

            EstadisticasFacturas estadisticas =
                new EstadisticasFacturas();

            foreach (RegistroFactura registro in registros)
            {
                estadisticas.TotalFacturas++;

                if (registro.Aprobada)
                {
                    estadisticas.FacturasAprobadas++;

                    estadisticas.MontoTotalAprobado +=
                        registro.Solicitud.TOTAL;

                    estadisticas.IvaTotalAprobado +=
                        registro.Solicitud.IVA;
                }
                else
                {
                    estadisticas.FacturasRechazadas++;

                    estadisticas.MontoTotalRechazado +=
                        registro.Solicitud.TOTAL;
                }
            }

            return estadisticas;
        }


        public List<RegistroFactura> ObtenerFacturasAprobadas()
        {
            List<RegistroFactura> registros = ObtenerRegistros();

            List<RegistroFactura> aprobadas =
                new List<RegistroFactura>();

            foreach (RegistroFactura registro in registros)
            {
                if (registro.Aprobada)
                {
                    aprobadas.Add(registro);
                }
            }

            return aprobadas;
        }

        public List<RegistroFactura> BuscarPorFecha(DateTime fecha)
        {
            List<RegistroFactura> registros = ObtenerRegistros();

            List<RegistroFactura> resultados =
                new List<RegistroFactura>();

            foreach (RegistroFactura registro in registros)
            {
                if (registro.FechaEmision.Date == fecha.Date)
                {
                    resultados.Add(registro);
                }
            }

            return resultados;
        }



        // CONSULTAR TODAS LAS FACTURAS

        public List<RegistroFactura> ObtenerFacturas()
        {
            return ObtenerRegistros();
        }

        // CONSULTAR FACTURAS RECHAZADAS

        public List<RegistroFactura> ObtenerFacturasRechazadas()
        {
            List<RegistroFactura> registros = ObtenerRegistros();

            List<RegistroFactura> rechazadas =
                new List<RegistroFactura>();

            foreach (RegistroFactura registro in registros)
            {
                if (!registro.Aprobada)
                {
                    rechazadas.Add(registro);
                }
            }

            return rechazadas;
        }

        // BUSCAR FACTURA APROBADA POR REFERENCIA

        public RegistroFactura? BuscarPorReferencia(
            string referencia)
        {
            List<RegistroFactura> registros = ObtenerRegistros();

            return registros.Find(r =>
                r.Aprobada &&
                string.Equals(
                    r.Solicitud.REFERENCIA,
                    referencia.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        // BUSCAR FACTURAS APROBADAS POR NIT

        public List<RegistroFactura> BuscarPorNIT(string nit)
        {
            List<RegistroFactura> registros = ObtenerRegistros();

            List<RegistroFactura> resultados =
                new List<RegistroFactura>();

            string nitBuscado = NormalizarNit(nit);

            foreach (RegistroFactura registro in registros)
            {
                if (!registro.Aprobada)
                {
                    continue;
                }

                string nitEmisor =
                    NormalizarNit(registro.Solicitud.NIT_EMISOR);

                string nitReceptor =
                    NormalizarNit(registro.Solicitud.NIT_RECEPTOR);

                if (nitEmisor == nitBuscado ||
                    nitReceptor == nitBuscado)
                {
                    resultados.Add(registro);
                }
            }

            return resultados;
        }

        // LEER ARCHIVO JSON

        private List<RegistroFactura> ObtenerRegistros()
        {
            string contenido = File.ReadAllText(rutaArchivo);

            return JsonSerializer.Deserialize<List<RegistroFactura>>(
                contenido) ?? new List<RegistroFactura>();
        }

        // GUARDAR ARCHIVO JSON

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

        // NORMALIZAR NIT

        private static string NormalizarNit(string? nit)
        {
            return (nit ?? string.Empty)
                .Trim()
                .Replace("-", "")
                .ToUpperInvariant();
        }

        // OBTENER SIGUIENTE CORRELATIVO

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
