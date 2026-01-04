using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StarkAid.Api.Helpers
{
    public static class TextHelper
    {
        public static string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            // 1. Lowercase
            var normalizado = texto.ToLowerInvariant();

            // 2. Remover acentos
            var fixedString = new string(normalizado
                .Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                .ToArray())
                .Normalize(NormalizationForm.FormC);

            // 3. Remover pontuação e caracteres especiais (deixar apenas letras, números e espaços)
            var sb = new StringBuilder();
            foreach (char c in fixedString)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                }
            }

            // 4. Remover espaços extras
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        public static string NormalizarParaBusca(string texto)
        {
            var normalizado = NormalizarTexto(texto);
            if (string.IsNullOrWhiteSpace(normalizado)) return string.Empty;

            // Stopwords comuns em Pt-Br (artigos, preposições curtas, conjunções de ligação simples)
            // Removendo: o, a, os, as, um, uns, uma, umas (artigos)
            // de, do, da, dos, das, em, no, na, nos, nas, por, para, com (preposições)
            // e, que (conjunções/pronomes relativos comuns que não alteram sentido da busca global)
            // "eh" (comum para 'é' sem acento)
            var stopwords = new HashSet<string> { 
                "o", "a", "os", "as", "um", "uns", "uma", "umas", 
                "de", "do", "da", "dos", "das", 
                "em", "no", "na", "nos", "nas", 
                "por", "para", "com", 
                "que", "e", "eh" 
            };

            var palavras = normalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var filtradas = palavras.Where(p => !stopwords.Contains(p));
            
            return string.Join(" ", filtradas);
        }

        public static bool EhAmbiguo(string textoOriginal)
        {
            var texto = NormalizarTexto(textoOriginal);
            if (string.IsNullOrWhiteSpace(texto)) return true;

            // 1. Perguntas factuais explícitas NÃO são ambíguas
            var perguntasUniversais = new[]
            {
                "quem descobriu",
                "quem inventou",
                "o que e",
                "qual e",
                "quando aconteceu",
                "onde fica",
                "para que serve"
            };

            if (perguntasUniversais.Any(p => texto.StartsWith(p)))
                return false;

            // 2. Nome ou termo isolado (alto risco)
            var palavras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (palavras.Length <= 2)
                return true;

            // 3. Não contém verbo de estado ou localização
            var verbos = new[]
            {
                " e ", " foi ", " sao ", " fica ", " serve ",
                " significa ", " funciona ", " existe "
            };

            // Adicionando espaços para evitar match parcial no meio de palavras
            var textoComEspacos = $" {texto} ";
            if (!verbos.Any(v => textoComEspacos.Contains(v)))
                return true;

            return false;
        }

        public static string LimparGirias(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            var girias = new[]
            {
                "parceiro", "mano", "brother", "parca", "mermo", "ta ligado", "tá ligado", "pode crer", 
                "caraca", "ja e", "já é", "coe", "coé", "maneiro", "bolado", "deu ruim", "papo reto", 
                "se liga", "mete o pe", "mete o pé", "vamo marcar", "crowdeado", 
                "pela saco", "tirar onda", "dar uma moral", "troca uma ideia",
                "veja bem", "entenda", "olha", "né", "ne", "saca", "tipo",
                "beleza", "blz", "viu", "entendeu"
            };

            var resultado = texto;
            foreach (var giria in girias)
            {
                // Regex melhorado:
                // 1. \b garante palavra inteira
                // 2. [ \t,.;]* remove espaços ou pontuação ANTES (opcional)
                // 3. [,.;!?]* remove pontuação DEPOIS (opcional)
                resultado = Regex.Replace(resultado, $@"\b{giria}\b[,.!?;:]*", "", RegexOptions.IgnoreCase);
            }

            // Limpeza de espaços duplos e pontuação órfã no final ou meio
            resultado = Regex.Replace(resultado, @"\s+", " ");
            
            // Remover vírgulas ou pontos isolados que ficaram antes de espaços ou fim de frase
            // Ex: "Colombo é o mais famoso, " -> "Colombo é o mais famoso"
            resultado = Regex.Replace(resultado, @"\s*[,.!?;:]+\s*$", "");
            resultado = Regex.Replace(resultado, @"\s+[,.!?;:]+", " ");

            resultado = resultado.Trim();

            // Se a frase começar com pontuação após a limpeza, remove
            resultado = Regex.Replace(resultado, @"^[.,!?;:]+\s*", "");

            // Capitalizar primeira letra se necessário
            if (resultado.Length > 0 && char.IsLower(resultado[0]))
            {
                resultado = char.ToUpper(resultado[0]) + resultado.Substring(1);
            }

            // Adicionar ponto final se terminar sem nada e tiver conteúdo
            if (resultado.Length > 3 && !Regex.IsMatch(resultado, @"[.!?]$"))
            {
                resultado += ".";
            }

            return resultado;
        }

        public static string NormalizarToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            token = token.ToLowerInvariant();

            // Regras de Stemming leve para Português (Remoção de sufixos comuns)
            if (token.EndsWith("dor") || token.EndsWith("cao")) // cao por causa da normalização que remove acentos
                return token[..^3];

            if (token.EndsWith("ou") || token.EndsWith("iu") || token.EndsWith("os") || token.EndsWith("as") || token.EndsWith("es"))
                return token[..^2];
            
            if (token.EndsWith("ar") || token.EndsWith("er") || token.EndsWith("ir") || token.EndsWith("am") || token.EndsWith("em"))
                return token[..^2];

            return token;
        }

        public static bool EhFollowUp(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            var normalize = input.ToLowerInvariant();
            // Adicionado "e " para pegar casos como "E madeira?"
            string[] followUpIndicators = { "porque", "por que", "e se", "porem", "mas", "entao", "e agora", "como assim", "sim", "nao", "obrigado", "valeu", "e " };
            return followUpIndicators.Any(indicator => normalize.StartsWith(indicator));
        }

        public static bool EhConteudoPessoal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            var normalize = input.ToLowerInvariant();
            // Palavras que indicam conteúdo pessoal ou temporal específico do usuário
            string[] personalIndicators = { 
                "meu", "minha", "meus", "minhas", "comigo", "eu ", "nosso", "nossa", 
                "estou", "sou", "tenho", "fui", "vou", 
                "lembre", "lembra", "falei", "disse", 
                "ontem", "hoje", "amanha", "agora", "antes", "depois",
                // Dados sensíveis devem sempre cair no escopo Usuario
                "senha", "wifi", "chave", "endereco", "cpf", "rg", "cartao", "credito", "debito",
                "telefone", "celular", "email", "banco", "agencia", "conta", "pix"
            };
            
            // Verifica palavra inteira ou início/fim de frase
            return personalIndicators.Any(p => normalize.Contains(p));
        }
        public static double JaccardSimilarity(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;

            // Aplica normalização morfológica (Stemming) em cada token antes de comparar
            var setA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Select(NormalizarToken)
                       .Where(t => t.Length > 0)
                       .ToHashSet();

            var setB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Select(NormalizarToken)
                       .Where(t => t.Length > 0)
                       .ToHashSet();

            var intersection = setA.Intersect(setB).Count();
            var union = setA.Union(setB).Count();

            return union == 0 ? 0 : (double)intersection / union;
        }

        public static double LevenshteinSimilarity(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;
            
            int distance = LevenshteinDistance(a, b);
            int maxLen = Math.Max(a.Length, b.Length);

            return maxLen == 0 ? 1.0 : 1.0 - (double)distance / maxLen;
        }

        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
