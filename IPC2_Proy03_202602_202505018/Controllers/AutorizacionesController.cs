
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
