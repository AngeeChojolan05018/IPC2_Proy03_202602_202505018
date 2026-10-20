
using System.Text.RegularExpressions;

namespace IPC2_Proy03_202602_202505018.Servicios
{
    public class ValidadorNIT
    {
        private static readonly Regex Formato =
            new Regex(@"^([0-9]{1,20})([0-9K])$");

        public bool EsValido(string? nit)
        {
            string nitNormalizado = (nit ?? string.Empty)
                .Trim()
                .Replace("-", "")
                .ToUpperInvariant();

            Match coincidencia = Formato.Match(nitNormalizado);

            if (!coincidencia.Success)
            {
                return false;
            }

            string cuerpo = coincidencia.Groups[1].Value;
            char verificadorIngresado =
                coincidencia.Groups[2].Value[0];

            int suma = 0;
            int posicion = 2;

            for (int i = cuerpo.Length - 1; i >= 0; i--)
            {
                int digito = cuerpo[i] - '0';
                suma += digito * posicion;
                posicion++;
            }

            int resultado = (11 - (suma % 11)) % 11;

            char verificadorCalculado = resultado == 10
                ? 'K'
                : (char)('0' + resultado);

            return verificadorIngresado == verificadorCalculado;
        }
    }
}
