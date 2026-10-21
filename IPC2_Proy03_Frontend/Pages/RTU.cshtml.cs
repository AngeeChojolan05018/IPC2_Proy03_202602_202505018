
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace IPC2_Proy03_Frontend.Pages
{
    public class RTUModel : PageModel
    {
        private readonly IHttpClientFactory clienteFactory;

        public RTUModel(IHttpClientFactory clienteFactory)
        {
            this.clienteFactory = clienteFactory;
        }

        public string ContribuyentesJson { get; set; } = "[]";

        public string Mensaje { get; set; } = "";

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        public async Task OnGetAsync()
        {
            await CargarContribuyentes();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Archivo == null || Archivo.Length == 0)
            {
                Mensaje = "Selecciona un archivo JSON.";
                await CargarContribuyentes();
                return Page();
            }

            try
            {
                using StreamReader lector =
                    new StreamReader(Archivo.OpenReadStream());

                string contenido = await lector.ReadToEndAsync();

                using JsonDocument documento =
                    JsonDocument.Parse(contenido);

                if (documento.RootElement.ValueKind !=
                    JsonValueKind.Array)
                {
                    Mensaje = "El archivo debe contener un arreglo JSON.";
                    await CargarContribuyentes();
                    return Page();
                }

                HttpClient cliente = clienteFactory.CreateClient("ApiFacturas");

                using StringContent cuerpo = new StringContent(
                    contenido,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage respuesta =
                    await cliente.PostAsync("api/RTU", cuerpo);

                string resultado =
                    await respuesta.Content.ReadAsStringAsync();

                Mensaje = respuesta.IsSuccessStatusCode
                    ? "RTU actualizado correctamente. " + resultado
                    : "Error al actualizar RTU: " + resultado;
            }
            catch (Exception ex)
            {
                Mensaje = "Error: " + ex.Message;
            }

            await CargarContribuyentes();
            return Page();
        }

        private async Task CargarContribuyentes()
        {
            try
            {
                HttpClient cliente = clienteFactory.CreateClient("ApiFacturas");

                ContribuyentesJson =
                    await cliente.GetStringAsync("api/RTU");
            }
            catch (Exception ex)
            {
                Mensaje += " No se pudo consultar el RTU: " + ex.Message;
            }
        }
    }
}
