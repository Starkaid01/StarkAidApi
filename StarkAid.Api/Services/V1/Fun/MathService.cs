using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace StarkAid.Api.Services.V1.Fun
{
    public interface IMathService
    {
        (bool Success, string Result) TryCalculate(string input);
    }

    public class MathService : IMathService
    {
        public (bool Success, string Result) TryCalculate(string input)
        {
            try
            {
                var normalized = NormalizeEquation(input);
                if (!Regex.IsMatch(normalized, @"\d"))
                    return (false, "Sem números.");

                double finalResult = 0;

                // Requirement: "381 mais 23 dividido por 2" => 202
                // This means sequential evaluation for natural language (left to right).
                // But we must also support parentheses: "(5 + 3) * 7" => 56
                
                if (normalized.Contains("(") || normalized.Contains(")"))
                {
                    // Use standard math precedence if parentheses are explicit
                    using var dt = new DataTable();
                    var result = dt.Compute(normalized.Replace(",", "."), null);
                    finalResult = Convert.ToDouble(result);
                }
                else
                {
                    // Sequential evaluation for natural language flow
                    finalResult = EvaluateSequential(normalized);
                }

                return (true, finalResult.ToString("G", new CultureInfo("pt-BR"))); 
            }
            catch (Exception ex)
            {
                return (false, $"Erro: {ex.Message}");
            }
        }

        private double EvaluateSequential(string expression)
        {
            // Simple sequential evaluator: left to right
            // First, sanitize: spaces and decimals
            expression = expression.Replace(",", ".");
            
            // Regex to split by operators but keeping them
            var parts = Regex.Split(expression, @"([\+\-\*\/\%])");
            if (parts.Length == 0) return 0;

            if (!double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return 0;

            for (int i = 1; i < parts.Length - 1; i += 2)
            {
                string op = parts[i];
                if (!double.TryParse(parts[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out double nextVal))
                    continue;

                result = op switch
                {
                    "+" => result + nextVal,
                    "-" => result - nextVal,
                    "*" => result * nextVal,
                    "/" => nextVal != 0 ? result / nextVal : 0,
                    "%" => result % nextVal,
                    _ => result
                };
            }

            return result;
        }

        private string NormalizeEquation(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            var processed = text.ToLowerInvariant();

            processed = processed.Replace("quanto é", "").Replace("calcule", "").Trim();

            // Word to digit conversion (basics 1-10)
            var words = new Dictionary<string, string>
            {
                {"um", "1"}, {"uma", "1"}, 
                {"dois", "2"}, {"duas", "2"},
                {"três", "3"}, 
                {"quatro", "4"}, 
                {"cinco", "5"}, 
                {"seis", "6"}, 
                {"sete", "7"}, 
                {"oito", "8"}, 
                {"nove", "9"}, 
                {"dez", "10"}
            };

            foreach (var kvp in words)
            {
                // Replace as whole words only
                processed = Regex.Replace(processed, $@"\b{kvp.Key}\b", kvp.Value);
            }

            // Word to Symbol mapping
            processed = processed.Replace(" mais ", "+");
            processed = processed.Replace(" menos ", "-");
            processed = processed.Replace(" vezes ", "*");
            processed = processed.Replace(" multiplicado por ", "*");
            processed = processed.Replace(" x ", "*"); // Handle "2 x 4"
            processed = processed.Replace(" dividido por ", "/");
            processed = processed.Replace(" dividido ", "/");
            processed = processed.Replace(" por cento de ", "%of");
            processed = processed.Replace(" por cento ", "%");

            // Handle "X % of Y" logic (Requirement: 15% de 340)
            string percentPattern = @"(\d+(?:[\.,]\d+)?)\s*(?:%|%of|de)\s*(\d+(?:[\.,]\d+)?)";
            processed = Regex.Replace(processed, percentPattern, m => {
                var p = m.Groups[1].Value.Replace(",", ".");
                var v = m.Groups[2].Value.Replace(",", ".");
                return $"({p} * {v} / 100)";
            });

            // Clean spaces but keep operators
            processed = Regex.Replace(processed, @"\s+", "");
            
            return processed;
        }
    }
}
