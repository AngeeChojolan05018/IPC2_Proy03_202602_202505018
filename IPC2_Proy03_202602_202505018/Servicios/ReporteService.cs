
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
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
            HashSet<string> emisores = new();
            HashSet<string> receptores = new();
            int emisorInvalido = 0, receptorInvalido = 0;
            int emisorInexistente = 0, receptorInexistente = 0;
            int ivaIncorrecto = 0, totalIncorrecto = 0;
            int referenciaDuplicada = 0, referenciaInvalida = 0;
            int fechaInvalida = 0, valorInvalido = 0;

            foreach (RegistroFactura registro in registros)
            {
                DateTime? fechaRegistro = ObtenerFechaParaReporte(registro);
                if (!fechaRegistro.HasValue || fechaRegistro.Value.Date < inicio.Date ||
                    fechaRegistro.Value.Date > fin.Date)
                    continue;

                string clave = fechaRegistro.Value.ToString(
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
                ErroresFactura errores = registro.Errores ?? new ErroresFactura();
                if (errores.NitEmisorInvalido) { emisorInvalido++; dia.NitEmisorInvalido++; }
                if (errores.NitReceptorInvalido) { receptorInvalido++; dia.NitReceptorInvalido++; }
                if (errores.NitEmisorInexistente) { emisorInexistente++; dia.NitEmisorInexistente++; }
                if (errores.NitReceptorInexistente) { receptorInexistente++; dia.NitReceptorInexistente++; }
                if (errores.IvaMalCalculado) { ivaIncorrecto++; dia.IvaMalCalculado++; }
                if (errores.TotalMalCalculado) { totalIncorrecto++; dia.TotalMalCalculado++; }
                if (errores.ReferenciaDuplicada) { referenciaDuplicada++; dia.ReferenciaDuplicada++; }
                if (errores.ReferenciaInvalida) { referenciaInvalida++; dia.ReferenciaInvalida++; }
                if (errores.FechaInvalida) { fechaInvalida++; dia.FechaInvalida++; }
                if (errores.ValorInvalido) { valorInvalido++; dia.ValorInvalido++; }
                string emisor = NormalizarNit(registro.Solicitud.NIT_EMISOR);
                string receptor = NormalizarNit(registro.Solicitud.NIT_RECEPTOR);
                if (emisor.Length > 0) { emisores.Add(emisor); dia.Emisores.Add(emisor); }
                if (receptor.Length > 0) { receptores.Add(receptor); dia.Receptores.Add(receptor); }
                dia.TotalFacturas++;
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
                FacturasSinErrores = aprobadas,
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
                },
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
            Dictionary<string, DetalleContribuyenteDia> porDia = new();

            foreach (RegistroFactura registro in registros)
            {
                if (!registro.Aprobada ||
                    !EnRango(registro, inicio, fin))
                {
                    continue;
                }

                Factura factura = registro.Solicitud;
                string clave = registro.FechaEmision.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (!porDia.ContainsKey(clave))
                    porDia[clave] = new DetalleContribuyenteDia { Fecha = clave };
                DetalleContribuyenteDia dia = porDia[clave];

                if (NormalizarNit(factura.NIT_EMISOR) == nitBuscado)
                {
                    emitidas++;
                    dia.CantidadEmitidas++;
                    dia.MontoEmitido += factura.TOTAL;
                    montoEmitido += factura.TOTAL;
                    ivaEmitido += factura.IVA;

                    facturasEmitidas.Add(registro);
                }

                if (NormalizarNit(factura.NIT_RECEPTOR) == nitBuscado)
                {
                    recibidas++;
                    dia.CantidadRecibidas++;
                    dia.MontoRecibido += factura.TOTAL;
                    montoRecibido += factura.TOTAL;
                    ivaRecibido += factura.IVA;

                    facturasRecibidas.Add(registro);
                }
            }

            List<DetalleContribuyenteDia> detalleDiario = new(porDia.Values);
            detalleDiario.RemoveAll(d => d.CantidadEmitidas == 0 && d.CantidadRecibidas == 0);
            detalleDiario.Sort((a, b) => string.CompareOrdinal(a.Fecha, b.Fecha));

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

                DetallePorDia = detalleDiario,
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


        // RESUMEN DE IVA POR FECHA Y NIT

        public object ResumenIvaPorFecha(DateTime fecha)
        {
            List<RegistroFactura> registros =
                autorizaciones.ObtenerFacturas();

            Dictionary<string, ResumenIvaNit> resumen = new();

            foreach (RegistroFactura registro in registros)
            {
                if (!registro.Aprobada ||
                    registro.FechaEmision.Date != fecha.Date)
                {
                    continue;
                }

                Factura factura = registro.Solicitud;

                string emisor = NormalizarNit(factura.NIT_EMISOR);
                string receptor = NormalizarNit(factura.NIT_RECEPTOR);

                if (!resumen.ContainsKey(emisor))
                {
                    resumen[emisor] = new ResumenIvaNit
                    {
                        NIT = emisor
                    };
                }

                if (!resumen.ContainsKey(receptor))
                {
                    resumen[receptor] = new ResumenIvaNit
                    {
                        NIT = receptor
                    };
                }

                resumen[emisor].IvaEmitido += factura.IVA;
                resumen[receptor].IvaRecibido += factura.IVA;
            }

            List<ResumenIvaNit> detalle = new(resumen.Values);

            detalle.Sort((a, b) =>
                string.CompareOrdinal(a.NIT, b.NIT));

            return new
            {
                Fecha = fecha.ToString("yyyy-MM-dd"),
                Detalle = detalle
            };
        }

        public class ResumenIvaNit
        {
            public string NIT { get; set; } = "";

            public decimal IvaEmitido { get; set; }

            public decimal IvaRecibido { get; set; }
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
            Match coincidencia = Regex.Match(tiempo,
                @"(?<!\d)\d{1,2}/\d{1,2}/\d{4}\s+\d{1,2}:\d{2}(?!\d)");
            if (!coincidencia.Success) return null;
            string[] formatos = { "d/M/yyyy H:mm", "dd/MM/yyyy HH:mm",
                "d/MM/yyyy H:mm", "dd/M/yyyy HH:mm" };
            return DateTime.TryParseExact(coincidencia.Value, formatos,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha)
                ? fecha : null;
        }

        // NORMALIZA EL NIT
        private static string NormalizarNit(string? nit)
        {
            return (nit ?? "")
                .Trim()
                .Replace("-", "")
                .ToUpperInvariant();
        }

        public class DetalleContribuyenteDia
        {
            public string Fecha { get; set; } = "";
            public int CantidadEmitidas { get; set; }
            public int CantidadRecibidas { get; set; }
            public decimal MontoEmitido { get; set; }
            public decimal MontoRecibido { get; set; }
        }

        // MODELO DEL RESUMEN DIARIO
        public class ResumenDia
        {
            public string Fecha { get; set; } = "";

            public int TotalFacturas { get; set; }
            public int FacturasSinErrores => Aprobadas;
            public HashSet<string> Emisores { get; set; } = new();
            public HashSet<string> Receptores { get; set; } = new();
            public int EmisoresDistintos => Emisores.Count;
            public int ReceptoresDistintos => Receptores.Count;
            public int NitEmisorInvalido { get; set; }
            public int NitReceptorInvalido { get; set; }
            public int NitEmisorInexistente { get; set; }
            public int NitReceptorInexistente { get; set; }
            public int IvaMalCalculado { get; set; }
            public int TotalMalCalculado { get; set; }
            public int ReferenciaDuplicada { get; set; }
            public int ReferenciaInvalida { get; set; }
            public int FechaInvalida { get; set; }
            public int ValorInvalido { get; set; }
            public int Aprobadas { get; set; }

            public int Rechazadas { get; set; }

            public decimal MontoSinIva { get; set; }

            public decimal MontoConIva { get; set; }

            public decimal Iva { get; set; }
        }
    }
}
