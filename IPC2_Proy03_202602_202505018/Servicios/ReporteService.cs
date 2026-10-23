
using System;
using System.Collections.Generic;
using System.Globalization;
using IPC2_Proy03_202602_202505018.Modelos;

namespace IPC2_Proy03_202602_202505018.Servicios
{
    public class ReporteService
    {
        private readonly AutorizacionService autorizaciones;

        public ReporteService(AutorizacionService autorizaciones)
        {
            this.autorizaciones = autorizaciones;
        }

        // REPORTE 1: RESUMEN POR RANGO DE FECHAS
        public object ReportePorFechas(DateTime inicio, DateTime fin)
        {
            List<RegistroFactura> registros =
                autorizaciones.ObtenerFacturas();

            int aprobadas = 0;
            int rechazadas = 0;

            decimal montoSinIva = 0;
            decimal montoConIva = 0;
            decimal iva = 0;

            Dictionary<string, ResumenDia> dias = new();

            foreach (RegistroFactura registro in registros)
            {
                if (!EnRango(registro, inicio, fin))
                    continue;

                string clave = registro.FechaEmision.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture);

                if (!dias.ContainsKey(clave))
                {
                    dias[clave] = new ResumenDia
                    {
                        Fecha = clave
                    };
                }

                ResumenDia dia = dias[clave];

                if (registro.Aprobada)
                {
                    aprobadas++;
                    dia.Aprobadas++;

                    montoSinIva += registro.Solicitud.VALOR;
                    montoConIva += registro.Solicitud.TOTAL;
                    iva += registro.Solicitud.IVA;

                    dia.MontoSinIva += registro.Solicitud.VALOR;
                    dia.MontoConIva += registro.Solicitud.TOTAL;
                    dia.Iva += registro.Solicitud.IVA;
                }
                else
                {
                    rechazadas++;
                    dia.Rechazadas++;
                }
            }

            List<ResumenDia> detalle = new(dias.Values);

            detalle.Sort((a, b) =>
                string.CompareOrdinal(a.Fecha, b.Fecha));

            return new
            {
                FechaInicio = inicio.ToString("yyyy-MM-dd"),
                FechaFin = fin.ToString("yyyy-MM-dd"),
                TotalProcesadas = aprobadas + rechazadas,
                Aprobadas = aprobadas,
                Rechazadas = rechazadas,
                MontoSinIva = montoSinIva,
                MontoConIva = montoConIva,
                Iva = iva,
                DetallePorDia = detalle
            };
        }

        // REPORTE 2: FACTURAS POR CONTRIBUYENTE
        public object ReporteContribuyente(
            string nit,
            DateTime inicio,
            DateTime fin)
        {
            List<RegistroFactura> registros =
                autorizaciones.ObtenerFacturas();

            string nitBuscado = NormalizarNit(nit);

            int emitidas = 0;
            int recibidas = 0;

            decimal montoEmitido = 0;
            decimal montoRecibido = 0;
            decimal ivaEmitido = 0;
            decimal ivaRecibido = 0;

            List<RegistroFactura> facturasEmitidas = new();
            List<RegistroFactura> facturasRecibidas = new();

            foreach (RegistroFactura registro in registros)
            {
                if (!registro.Aprobada ||
                    !EnRango(registro, inicio, fin))
                {
                    continue;
                }

                Factura factura = registro.Solicitud;

                if (NormalizarNit(factura.NIT_EMISOR) == nitBuscado)
                {
                    emitidas++;
                    montoEmitido += factura.TOTAL;
                    ivaEmitido += factura.IVA;

                    facturasEmitidas.Add(registro);
                }

                if (NormalizarNit(factura.NIT_RECEPTOR) == nitBuscado)
                {
                    recibidas++;
                    montoRecibido += factura.TOTAL;
                    ivaRecibido += factura.IVA;

                    facturasRecibidas.Add(registro);
                }
            }

            return new
            {
                NIT = nitBuscado,

                FechaInicio = inicio.ToString("yyyy-MM-dd"),
                FechaFin = fin.ToString("yyyy-MM-dd"),

                CantidadEmitidas = emitidas,
                CantidadRecibidas = recibidas,

                MontoEmitido = montoEmitido,
                MontoRecibido = montoRecibido,

                IvaEmitido = ivaEmitido,
                IvaRecibido = ivaRecibido,

                DiferenciaIva = ivaEmitido - ivaRecibido,

                FacturasEmitidas = facturasEmitidas,
                FacturasRecibidas = facturasRecibidas
            };
        }

        // REPORTE 3: ESTADISTICAS DE ERRORES
        public object ReporteErrores(DateTime inicio, DateTime fin)
        {
            List<RegistroFactura> registros =
                autorizaciones.ObtenerFacturas();

            int total = 0;
            int aprobadas = 0;
            int rechazadas = 0;
            int sinErrores = 0;

            int emisorInvalido = 0;
            int receptorInvalido = 0;
            int emisorInexistente = 0;
            int receptorInexistente = 0;

            int ivaIncorrecto = 0;
            int totalIncorrecto = 0;

            int referenciaDuplicada = 0;
            int referenciaInvalida = 0;

            int fechaInvalida = 0;
            int valorInvalido = 0;

            HashSet<string> emisores = new();
            HashSet<string> receptores = new();

            foreach (RegistroFactura registro in registros)
            {
                // Intenta obtener la fecha registrada o la original.
                DateTime? fecha = ObtenerFechaParaReporte(registro);

                if (!fecha.HasValue ||
                    fecha.Value.Date < inicio.Date ||
                    fecha.Value.Date > fin.Date)
                {
                    continue;
                }

                total++;

                if (registro.Aprobada)
                    aprobadas++;
                else
                    rechazadas++;

                ErroresFactura error = registro.Errores
                    ?? new ErroresFactura();

                bool tieneErrores =
                    error.NitEmisorInvalido ||
                    error.NitReceptorInvalido ||
                    error.NitEmisorInexistente ||
                    error.NitReceptorInexistente ||
                    error.IvaMalCalculado ||
                    error.TotalMalCalculado ||
                    error.ReferenciaDuplicada ||
                    error.ReferenciaInvalida ||
                    error.FechaInvalida ||
                    error.ValorInvalido;

                if (!tieneErrores &&
                    string.IsNullOrWhiteSpace(registro.MotivoRechazo))
                {
                    sinErrores++;
                }

                if (error.NitEmisorInvalido)
                    emisorInvalido++;

                if (error.NitReceptorInvalido)
                    receptorInvalido++;

                if (error.NitEmisorInexistente)
                    emisorInexistente++;

                if (error.NitReceptorInexistente)
                    receptorInexistente++;

                if (error.IvaMalCalculado)
                    ivaIncorrecto++;

                if (error.TotalMalCalculado)
                    totalIncorrecto++;

                if (error.ReferenciaDuplicada)
                    referenciaDuplicada++;

                if (error.ReferenciaInvalida)
                    referenciaInvalida++;

                if (error.FechaInvalida)
                    fechaInvalida++;

                if (error.ValorInvalido)
                    valorInvalido++;

                string emisor = NormalizarNit(
                    registro.Solicitud.NIT_EMISOR);

                string receptor = NormalizarNit(
                    registro.Solicitud.NIT_RECEPTOR);

                if (!string.IsNullOrWhiteSpace(emisor))
                    emisores.Add(emisor);

                if (!string.IsNullOrWhiteSpace(receptor))
                    receptores.Add(receptor);
            }

            return new
            {
                FechaInicio = inicio.ToString("yyyy-MM-dd"),
                FechaFin = fin.ToString("yyyy-MM-dd"),

                TotalProcesadas = total,
                Aprobadas = aprobadas,
                Rechazadas = rechazadas,
                SinErrores = sinErrores,

                EmisoresDistintos = emisores.Count,
                ReceptoresDistintos = receptores.Count,

                Errores = new
                {
                    NitEmisorInvalido = emisorInvalido,
                    NitReceptorInvalido = receptorInvalido,

                    NitEmisorInexistente = emisorInexistente,
                    NitReceptorInexistente = receptorInexistente,

                    IvaMalCalculado = ivaIncorrecto,
                    TotalMalCalculado = totalIncorrecto,

                    ReferenciaDuplicada = referenciaDuplicada,
                    ReferenciaInvalida = referenciaInvalida,

                    FechaInvalida = fechaInvalida,
                    ValorInvalido = valorInvalido
                }
            };
        }

        // COMPRUEBA SI LA FACTURA ESTA DENTRO DEL RANGO
        private static bool EnRango(
            RegistroFactura registro,
            DateTime inicio,
            DateTime fin)
        {
            if (registro.FechaEmision == DateTime.MinValue)
                return false;

            DateTime fecha = registro.FechaEmision.Date;

            return fecha >= inicio.Date &&
                   fecha <= fin.Date;
        }

        // OBTIENE LA FECHA PARA EL REPORTE DE ERRORES
        private static DateTime? ObtenerFechaParaReporte(
            RegistroFactura registro)
        {
            if (registro.FechaEmision != DateTime.MinValue)
                return registro.FechaEmision;

            string tiempo = registro.Solicitud.TIEMPO ?? "";

            string fechaTexto = tiempo
                .Replace("Guatemala,", "")
                .Replace("hrs.", "")
                .Trim();

            bool valida = DateTime.TryParseExact(
                fechaTexto,
                "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fecha);

            if (valida)
                return fecha;

            // Una fecha imposible no puede asignarse
            // confiablemente a un rango de fechas.
            return null;
        }

        // NORMALIZA EL NIT
        private static string NormalizarNit(string? nit)
        {
            return (nit ?? "")
                .Trim()
                .Replace("-", "")
                .ToUpperInvariant();
        }

        // MODELO DEL RESUMEN DIARIO
        public class ResumenDia
        {
            public string Fecha { get; set; } = "";

            public int Aprobadas { get; set; }

            public int Rechazadas { get; set; }

            public decimal MontoSinIva { get; set; }

            public decimal MontoConIva { get; set; }

            public decimal Iva { get; set; }
        }
    }
}
