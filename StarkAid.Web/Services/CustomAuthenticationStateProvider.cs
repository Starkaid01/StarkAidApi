using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace StarkAid.Web.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;

        public CustomAuthenticationStateProvider(IJSRuntime js)
        {
            _js = js;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", "token");

            var identity = new ClaimsIdentity();

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
                }
                catch
                {
                    await _js.InvokeVoidAsync("localStorage.removeItem", "token");
                }
            }

            var user = new ClaimsPrincipal(identity);
            var state = new AuthenticationState(user);

            NotifyAuthenticationStateChanged(Task.FromResult(state));

            return state;
        }

        public void NotifyUserAuthentication(string token)
        {
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
        }

        public void NotifyUserLogout()
        {
            var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            NotifyAuthenticationStateChanged(authState);
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            foreach (var kvp in keyValuePairs)
            {
                var value = kvp.Value;
                var claimType = kvp.Key;

                // Map standard role claim keys to Microsoft's claim type reference
                if (claimType.Equals("role", StringComparison.OrdinalIgnoreCase) || 
                    claimType.Equals("actort", StringComparison.OrdinalIgnoreCase))
                {
                    claimType = ClaimTypes.Role;
                }

                if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            claims.Add(new Claim(claimType, item.ToString()));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(claimType, element.ToString()));
                    }
                }
                else
                {
                     claims.Add(new Claim(claimType, value.ToString()));
                }
            }

            return claims;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}
