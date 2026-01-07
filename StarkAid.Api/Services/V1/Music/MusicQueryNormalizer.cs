using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace StarkAid.Api.Services.V1.Music
{
    public static class MusicQueryNormalizer
    {
        public static string Normalize(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return string.Empty;

            // 1. Lowercase
            string text = query.ToLowerInvariant();

            // 2. Remove common prefixes
            text = Regex.Replace(text, @"^(toca|tocar|coloque|ouvir|quero ouvir|play)\s*", "");
            // text = Regex.Replace(text, @"\b(do|da|de|pelo|pela|com|e)\b", " "); // Mantemos para melhor precisão no YouTube

            // 3. Remove Accents
            text = RemoveAccents(text);

            // 4. Clean special characters
            text = Regex.Replace(text, @"[^a-z0-9\s]", " ");

            // 5. Remove extra spaces and sort words
            var tokens = Regex.Replace(text, @"\s+", " ").Trim().Split(' ');
            Array.Sort(tokens);
            text = string.Join(" ", tokens);

            return text;
        }

        private static string RemoveAccents(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
