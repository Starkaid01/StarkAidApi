using System.Text.RegularExpressions;

namespace StarkAid.Api.Services.WPPconnect
{
    public class Contato
    {
        public string Nome { get; set; }
        public string Numero { get; set; }
    }

    public static class ContatoFilter
    {
        // Regex para detectar letras (inclui acentos e caracteres latinos)
        private static readonly Regex TemLetra = new(@"[A-Za-zÀ-ÖØ-öø-ÿ]", RegexOptions.Compiled);

        public static List<Contato> FiltrarContatos(List<Contato> contatos)
        {
            return contatos
                .Where(c =>
                    !string.IsNullOrWhiteSpace(c.Nome) &&
                    c.Nome.Trim().Length > 1 &&
                    TemLetra.IsMatch(c.Nome))
                .ToList();
        }
    }
}
