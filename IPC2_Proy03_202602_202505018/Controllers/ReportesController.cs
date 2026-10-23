
using System;
using Microsoft.AspNetCore.Mvc;
using IPC2_Proy03_202602_202505018.Servicios;

namespace IPC2_Proy03_202602_202505018.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly ReporteService servicio;

        public ReportesController(ReporteService servicio)
        {
            this.servicio = servicio;
        }

        [HttpGet("fechas")]
        public IActionResult PorFechas(
            [FromQuery] DateTime inicio,
            [FromQuery] DateTime fin)
        {
            if (!FechasValidas(inicio, fin))
                return BadRequest("El rango de fechas es inválido.");

            return Ok(servicio.ReportePorFechas(inicio, fin));
        }

        [HttpGet("contribuyente")]
        public IActionResult Contribuyente(
            [FromQuery] string nit,
            [FromQuery] DateTime inicio,
            [FromQuery] DateTime fin)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return BadRequest("Debe ingresar un NIT.");

            if (!FechasValidas(inicio, fin))
                return BadRequest("El rango de fechas es inválido.");

            return Ok(servicio.ReporteContribuyente(nit, inicio, fin));
        }

        [HttpGet("errores")]
        public IActionResult Errores(
            [FromQuery] DateTime inicio,
            [FromQuery] DateTime fin)
        {
            if (!FechasValidas(inicio, fin))
                return BadRequest("El rango de fechas es inválido.");

            return Ok(servicio.ReporteErrores(inicio, fin));
        }

        private static bool FechasValidas(DateTime inicio, DateTime fin)
        {
            return inicio != DateTime.MinValue &&
                   fin != DateTime.MinValue &&
                   inicio.Date <= fin.Date;
        }
    }
}
