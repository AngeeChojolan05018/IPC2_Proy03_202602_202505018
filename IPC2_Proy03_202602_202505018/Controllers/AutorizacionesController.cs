
using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using IPC2_Proy03_202602_202505018.Modelos;
using IPC2_Proy03_202602_202505018.Servicios;

namespace IPC2_Proy03_202602_202505018.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutorizacionesController : ControllerBase
    {
        private readonly AutorizacionService servicio;

        public AutorizacionesController(
            AutorizacionService servicio)
        {
            this.servicio = servicio;
        }

        [HttpPost]
        public ActionResult<RespuestaAutorizaciones> Procesar(
            [FromBody] JsonElement contenido)
        {
            if (contenido.ValueKind != JsonValueKind.Array)
            {
                return BadRequest(new
                {
                    error = "Debe enviar un arreglo JSON de facturas."
                });
            }

            if (contenido.GetArrayLength() == 0)
            {
                return BadRequest(new
                {
                    error = "El arreglo de facturas esta vacio."
                });
            }

            string[] camposObligatorios =
            {
                "TIEMPO",
                "REFERENCIA",
                "NIT_EMISOR",
                "NIT_RECEPTOR",
                "VALOR",
                "IVA",
                "TOTAL"
            };

            foreach (JsonElement elemento in contenido.EnumerateArray())
            {
                if (elemento.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest(new
                    {
                        error = "Cada factura debe ser un objeto JSON."
                    });
                }

                foreach (string campo in camposObligatorios)
                {
                    if (!elemento.TryGetProperty(campo, out _))
                    {
                        return BadRequest(new
                        {
                            error = "Falta el campo obligatorio: " + campo
                        });
                    }
                }
            }

            List<Factura>? facturas;

            try
            {
                facturas = JsonSerializer.Deserialize<List<Factura>>(
                    contenido.GetRawText(),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (JsonException)
            {
                return BadRequest(new
                {
                    error = "El contenido de las facturas no es valido."
                });
            }

            if (facturas == null || facturas.Count == 0)
            {
                return BadRequest(new
                {
                    error = "Debe enviar al menos una factura."
                });
            }

            return Ok(servicio.Procesar(facturas));
        }

        [HttpGet]
        public ActionResult<List<RegistroFactura>> Obtener()
        {
            return Ok(servicio.ObtenerFacturas());
        }

        [HttpGet("estadisticas")]
        public ActionResult<EstadisticasFacturas> Estadisticas()
        {
            return Ok(servicio.ObtenerEstadisticas());
        }

        [HttpGet("aprobadas")]
        public ActionResult<List<RegistroFactura>> Aprobadas()
        {
            return Ok(servicio.ObtenerFacturasAprobadas());
        }

        [HttpGet("rechazadas")]
        public ActionResult<List<RegistroFactura>> Rechazadas()
        {
            return Ok(servicio.ObtenerFacturasRechazadas());
        }

        [HttpGet("fecha")]
        public ActionResult<List<RegistroFactura>> BuscarPorFecha(
            [FromQuery] DateTime fecha)
        {
            if (fecha == DateTime.MinValue)
            {
                return BadRequest(new
                {
                    error = "Debe ingresar una fecha valida."
                });
            }

            return Ok(servicio.BuscarPorFecha(fecha));
        }

        [HttpGet("buscar/{referencia}")]
        public ActionResult<RegistroFactura> Buscar(
            string referencia)
        {
            RegistroFactura? factura =
                servicio.BuscarPorReferencia(referencia);

            if (factura == null)
            {
                return NotFound(new
                {
                    error = "No se encontro una factura aprobada con esa referencia."
                });
            }

            return Ok(factura);
        }

        [HttpGet("nit/{nit}")]
        public ActionResult<List<RegistroFactura>> BuscarPorNIT(
            string nit)
        {
            return Ok(servicio.BuscarPorNIT(nit));
        }
    }
}
