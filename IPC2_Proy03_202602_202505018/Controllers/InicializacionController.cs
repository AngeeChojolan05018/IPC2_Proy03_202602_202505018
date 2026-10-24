
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace IPC2_Proy03_202602_202505018.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InicializacionController : ControllerBase
    {
        private readonly IWebHostEnvironment entorno;

        public InicializacionController(IWebHostEnvironment entorno)
        {
            this.entorno = entorno;
        }

        [HttpPost]
        public IActionResult Reiniciar()
        {
            string carpeta = Path.Combine(
                entorno.ContentRootPath,
                "Almacenamiento");

            Directory.CreateDirectory(carpeta);

            System.IO.File.WriteAllText(
    Path.Combine(carpeta, "facturas.json"),
    "[]");

            System.IO.File.WriteAllText(
    Path.Combine(carpeta, "rtu.json"),
    "[]");

            return Ok(new
            {
                mensaje = "Sistema inicializado correctamente."
            });
        }
    }
}
