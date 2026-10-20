using Microsoft.AspNetCore.Mvc;
using IPC2_Proy03_202602_202505018.Modelos;
using IPC2_Proy03_202602_202505018.Servicios;

namespace IPC2_Proy03_202602_202505018.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RTUController : ControllerBase
    {
        private readonly RTUService servicio;

        public RTUController(RTUService servicio)
        {
            this.servicio = servicio;
        }

        [HttpGet]
        public ActionResult<List<Contribuyente>> Obtener()
        {
            return Ok(servicio.ObtenerContribuyentes());
        }

        [HttpPost]
        public ActionResult<ResultadoActualizacionRTU> Actualizar(
            [FromBody] List<Contribuyente> contribuyentes)
        {
            if (contribuyentes == null)
            {
                return BadRequest("Debe enviar una lista de contribuyentes.");
            }

            ResultadoActualizacionRTU resultado =
                servicio.Actualizar(contribuyentes);

            return Ok(resultado);
        }
    }
}
