using System.Security.Cryptography;
using System.Text;

namespace StarkAid.Api.Features.TuyaAdmin.Services
{
    public static class TuyaSignHelper
    {
        // Hash SHA256 do conteúdo (body) — hex lowercase
        private static string ComputeSHA256(string content)
        {
            content ??= "";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        // HMAC-SHA256 -> hex lowercase
        private static string HmacSha256Hex(string text, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Build sign para requisições que exigem body (POST) ou sem body (GET).
        /// Usa: clientId + accessToken + t + stringToSign
        /// stringToSign = METHOD + "\n" + contentSHA256 + "\n\n" + pathAndQuery
        /// Retorna HMAC_SHA256(finalString, secret) em UPPERCASE no seu uso original,
        /// mas aqui retornamos lowercase para consistência interna — caller pode ToUpper() se quiser.
        /// </summary>
        public static string BuildSign(
            string clientId,
            string secret,
            string method,
            string pathAndQuery,
            string accessToken,
            string timestamp,
            string bodyJson = "")
        {
            var contentSha256 = ComputeSHA256(bodyJson);

            var stringToSign = $"{method.ToUpperInvariant()}\n{contentSha256}\n\n{pathAndQuery}";

            var finalString = $"{clientId}{accessToken}{timestamp}{stringToSign}";

            // A Tuya aceita hex em uppercase; seu código anterior usava ToUpper() no retorno.
            return HmacSha256Hex(finalString, secret).ToUpperInvariant();
        }

        /// <summary>
        /// Build sign específico para a requisição de token (GET /v1.0/token?grant_type=1).
        /// A assinatura do token NÃO inclui accessToken no finalString.
        /// finalString = clientId + timestamp + stringToSign
        /// </summary>
        public static string BuildTokenRequestSign(
            string clientId,
            string secret,
            string method,
            string pathAndQuery,
            string timestamp)
        {
            var contentSha256 = ComputeSHA256(""); // token request não tem body
            var stringToSign = $"{method.ToUpperInvariant()}\n{contentSha256}\n\n{pathAndQuery}";
            var finalString = $"{clientId}{timestamp}{stringToSign}";

            return HmacSha256Hex(finalString, secret).ToUpperInvariant();
        }
    }
}