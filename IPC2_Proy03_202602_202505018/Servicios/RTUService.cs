
using System.Text.Json;
using IPC2_Proy03_202602_202505018.Modelos;

namespace IPC2_Proy03_202602_202505018.Servicios
{
    public class RTUService
    {
        private readonly string rutaArchivo;
        private readonly ValidadorNIT validador;

        public RTUService(
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment entorno,
            ValidadorNIT validador)
        {
            rutaArchivo = Path.Combine(
                entorno.ContentRootPath,
                "Almacenamiento",
                "rtu.json");

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

        public List<Contribuyente> ObtenerContribuyentes()
        {
            string contenido = File.ReadAllText(rutaArchivo);

            return JsonSerializer.Deserialize<List<Contribuyente>>(
                contenido) ?? new List<Contribuyente>();
        }

        public ResultadoActualizacionRTU Actualizar(
            List<Contribuyente> contribuyentes)
        {
            List<Contribuyente> registrados = ObtenerContribuyentes();

            ResultadoActualizacionRTU resultado =
                new ResultadoActualizacionRTU();

            foreach (Contribuyente contribuyente in contribuyentes)
            {
                string nit = (contribuyente.NIT ?? string.Empty)
                    .Trim()
                    .Replace("-", "")
                    .ToUpperInvariant();

                if (!validador.EsValido(nit))
                {
                    resultado.NITS_INVALIDOS++;
                    continue;
                }

                Contribuyente? existente = registrados.Find(
                    c => c.NIT.Replace("-", "")
                        .Equals(nit, StringComparison.OrdinalIgnoreCase));

                contribuyente.NIT = nit;

                if (existente == null)
                {
                    registrados.Add(contribuyente);
                    resultado.CONTRIBUYENTES_NUEVOS++;
                }
                else
                {
                    existente.Nombre = contribuyente.Nombre;
                    existente.NIT = nit;
                    resultado.CONTRIBUYENTES_ACTUALIZADOS++;
                }
            }

            string json = JsonSerializer.Serialize(
                registrados,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(rutaArchivo, json);

            return resultado;
        }
    }
}
