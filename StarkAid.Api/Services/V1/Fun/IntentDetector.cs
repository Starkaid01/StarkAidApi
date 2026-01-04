using System;
using System.Text.RegularExpressions;

namespace StarkAid.Api.Services.V1.Fun
{
    public enum FunIntent
    {
        None,
        Math,
        Joke
    }

    public interface IIntentDetector
    {
        FunIntent DetectIntent(string text);
    }

    public class IntentDetector : IIntentDetector
    {
        public FunIntent DetectIntent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return FunIntent.None;

            var normalized = text.ToLowerInvariant();

            // Math Detection
            if (IsMath(normalized))
                return FunIntent.Math;

            // Joke Detection
            if (normalized.Contains("me conte uma piada") || 
                normalized.Contains("diga uma piada") || 
                normalized.Contains("conta uma piada") ||
                normalized.Contains("contar uma piada") ||
                normalized.Contains("fala uma piada") ||
                normalized.Contains("falar uma piada") ||
                normalized.Contains("piada"))
                return FunIntent.Joke;

            return FunIntent.None;
        }

        private bool IsMath(string text)
        {
            // Keywords or patterns like "quanto é", numbers + operators
            if (text.Contains("quanto é") || 
                text.Contains("vezes") || 
                text.Contains("dividido") || 
                text.Contains("mais") || 
                text.Contains("menos") ||
                text.Contains("calcula") ||
                text.Contains("soma") ||
                text.Contains("subtrai") ||
                text.Contains("multiplica") ||
                text.Contains("divide"))
                return true;

            // Regex for basic math symbols if no keywords
            if (Regex.IsMatch(text, @"[\d\s\(\)\+\-\*\/\%]"))
            {
                // Must have at least a balance of numbers and something else to not be just a number
                if (Regex.IsMatch(text, @"[\+\-\*\/\%]")) return true;
            }

            return false;
        }
    }
}
