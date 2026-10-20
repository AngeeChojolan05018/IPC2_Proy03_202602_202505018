
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

        public AutorizacionesController(AutorizacionService servicio)
        {
            this.servicio = servicio;
        }

        [HttpGet]
        public ActionResult<List<RegistroFactura>> Obtener()
        {
            return Ok(servicio.ObtenerFacturas());
        }

        [HttpGet("buscar/{referencia}")]
        public ActionResult<RegistroFactura> Buscar(string referencia)
        {
            RegistroFactura? factura =
                servicio.BuscarPorReferencia(referencia);

            if (factura == null)
            {
                return NotFound("No se encontró una factura aprobada con esa referencia.");
            }

            return Ok(factura);
        }

        [HttpGet("nit/{nit}")]
        public ActionResult<List<RegistroFactura>> BuscarPorNIT(string nit)
        {
            List<RegistroFactura> facturas =
                servicio.BuscarPorNIT(nit);

            return Ok(facturas);
        }

        [HttpGet("rechazadas")]
        public ActionResult<List<RegistroFactura>> ObtenerRechazadas()
        {
            List<RegistroFactura> facturas =
                servicio.ObtenerFacturasRechazadas();

            return Ok(facturas);
        }
        

        [HttpPost]
        public ActionResult<RespuestaAutorizaciones> Procesar(
            [FromBody] List<Factura> facturas)
        {
            if (facturas == null || facturas.Count == 0)
            {
                return BadRequest(
                    "Debe enviar una lista con al menos una factura.");
            }

            RespuestaAutorizaciones respuesta =
                servicio.Procesar(facturas);

            return Ok(respuesta);
        }
    }
}
