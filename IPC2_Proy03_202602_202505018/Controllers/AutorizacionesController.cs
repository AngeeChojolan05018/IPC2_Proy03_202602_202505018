
using System;
using System.Collections.Generic;
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
            [FromBody] List<Factura> facturas)
        {
            if (facturas == null || facturas.Count == 0)
            {
                return BadRequest(
                    "Debe enviar al menos una factura.");
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
                return BadRequest("Debe ingresar una fecha valida.");
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
                return NotFound(
                    "No se encontro una factura aprobada con esa referencia.");
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
