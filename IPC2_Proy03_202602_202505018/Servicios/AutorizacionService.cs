
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
        private static readonly object bloqueoArchivo = new object();
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
            lock (bloqueoArchivo)
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

                    System.Text.RegularExpressions.Match coincidencia =
                        System.Text.RegularExpressions.Regex.Match(
                            tiempo,
                            @"(?<!\d)\d{1,2}/\d{1,2}/\d{4}\s+\d{1,2}:\d{2}(?!\d)");

                    string fechaTexto = coincidencia.Success
                        ? coincidencia.Value
                        : string.Empty;

                    string[] formatos =
                                    {
                    "d/M/yyyy H:mm",
                    "dd/MM/yyyy HH:mm",
                    "d/MM/yyyy H:mm",
                    "dd/M/yyyy HH:mm"
                };

                    if (DateTime.TryParseExact(
                        fechaTexto,
                        formatos,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime fechaEmision))
                    {
                        registro.FechaEmision = fechaEmision;
                    }
                    else
                    {
                        respuesta.Resultado.CantidadFacturasDescartadas++;
                        registro.MotivoRechazo = "Fecha de emisión inválida.";
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
                        respuesta.Resultado.CantidadRechazadasPorReferenciaInvalida++;

                        registro.MotivoRechazo =
                            "La referencia debe tener entre 1 y 40 caracteres.";

                        registros.Add(registro);
                        continue;
                    }

                    bool referenciaDuplicada = registros.Exists(
                        r => r.Aprobada && string.Equals(
                            r.Solicitud.REFERENCIA,
                            referencia,
                            StringComparison.OrdinalIgnoreCase));

                    if (referenciaDuplicada)
                    {
                        registro.Errores.ReferenciaDuplicada = true;
                        respuesta.Resultado.CantidadRechazadasPorReferenciaDuplicada++;

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

                    registro.Errores.NitEmisorInvalido =
                        !emisorValido;

                    registro.Errores.NitReceptorInvalido =
                        !receptorValido;

                    // COMPROBAR EXISTENCIA EN RTU

                    if (emisorValido)
                    {
                        registro.Errores.NitEmisorInexistente =
                            !contribuyentes.Exists(
                                c => NormalizarNit(c.NIT) == nitEmisor);
                    }

                    if (receptorValido)
                    {
                        registro.Errores.NitReceptorInexistente =
                            !contribuyentes.Exists(
                                c => NormalizarNit(c.NIT) == nitReceptor);
                    }

                    // VALIDAR VALOR

                    registro.Errores.ValorInvalido =
                        factura.VALOR <= 0;

                    // VALIDAR IVA Y TOTAL

                    decimal ivaEsperado = decimal.Round(
                        factura.VALOR * 0.12m,
                        2,
                        MidpointRounding.AwayFromZero);

                    decimal totalEsperado =
                        factura.VALOR + ivaEsperado;

                    registro.Errores.IvaMalCalculado =
                        factura.IVA != ivaEsperado;

                    registro.Errores.TotalMalCalculado =
                        factura.TOTAL != totalEsperado;

                    // ACTUALIZAR CONTADORES

                    if (registro.Errores.NitEmisorInvalido)
                        respuesta.Resultado.CantidadRechazadasPorEmisorInvalido++;

                    if (registro.Errores.NitReceptorInvalido)
                        respuesta.Resultado.CantidadRechazadasPorReceptorInvalido++;

                    if (registro.Errores.NitEmisorInexistente)
                        respuesta.Resultado.CantidadRechazadasPorEmisorInexistente++;

                    if (registro.Errores.NitReceptorInexistente)
                        respuesta.Resultado.CantidadRechazadasPorReceptorInexistente++;

                    if (registro.Errores.ValorInvalido)
                        respuesta.Resultado.CantidadRechazadasPorValorInvalido++;

                    if (registro.Errores.IvaMalCalculado)
                        respuesta.Resultado.CantidadRechazadasPorIvaIncorrecto++;

                    if (registro.Errores.TotalMalCalculado)
                        respuesta.Resultado.CantidadRechazadasPorTotalIncorrecto++;

                    // COMPROBAR SI EXISTEN ERRORES

                    bool tieneErrores =
                        registro.Errores.NitEmisorInvalido ||
                        registro.Errores.NitReceptorInvalido ||
                        registro.Errores.NitEmisorInexistente ||
                        registro.Errores.NitReceptorInexistente ||
                        registro.Errores.ValorInvalido ||
                        registro.Errores.IvaMalCalculado ||
                        registro.Errores.TotalMalCalculado;

                    if (tieneErrores)
                    {
                        if (registro.Errores.NitEmisorInvalido ||
                            registro.Errores.NitReceptorInvalido)
                        {
                            registro.MotivoRechazo = "NIT inválido.";
                        }
                        else if (registro.Errores.NitEmisorInexistente ||
                                 registro.Errores.NitReceptorInexistente)
                        {
                            registro.MotivoRechazo =
                                "El emisor o el receptor no existe en el RTU.";
                        }
                        else if (registro.Errores.ValorInvalido)
                        {
                            registro.MotivoRechazo =
                                "El valor de la factura debe ser mayor que cero.";
                        }
                        else
                        {
                            registro.MotivoRechazo =
                                "El IVA o el total son incorrectos.";
                        }

                        registros.Add(registro);
                        continue;
                    }


                    // GENERAR CODIGO DE APROBACION

                    int correlativo = ObtenerSiguienteCorrelativo(
                        registros,
                        fechaActual);

                    if (correlativo > 99999999)
                    {
                        registro.MotivoRechazo = "Se agotaron los correlativos del día.";
                        registros.Add(registro);
                        continue;
                    }

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
            lock (bloqueoArchivo)
            {
                string contenido = File.ReadAllText(rutaArchivo);
                return JsonSerializer.Deserialize<List<RegistroFactura>>(
                    contenido) ?? new List<RegistroFactura>();
            }
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

            lock (bloqueoArchivo)
            {
                File.WriteAllText(rutaArchivo, json);
            }
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
