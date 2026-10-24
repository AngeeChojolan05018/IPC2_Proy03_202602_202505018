
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IPC2_Proy03_Frontend.Pages
{
    public class InicializarModel : PageModel
    {
        private readonly IHttpClientFactory factory;

        public InicializarModel(IHttpClientFactory factory)
        {
            this.factory = factory;
        }

        public string Mensaje { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                HttpClient cliente =
                    factory.CreateClient("ApiFacturas");

                HttpResponseMessage respuesta =
                    await cliente.PostAsync(
                        "api/Inicializacion",
                        null);

                if (respuesta.IsSuccessStatusCode)
                {
                    Mensaje =
                        "Sistema inicializado: RTU y facturas vacíos.";
                }
                else
                {
                    Mensaje = "Error: " +
                        await respuesta.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Mensaje =
                    "No se pudo conectar a la API: " + ex.Message;
            }

            return Page();
        }
    }
}
