using StarkAid.Web.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

public class ApiKeyAndBearerHandler : DelegatingHandler
{
    public ApiKeyAndBearerHandler()
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Adiciona Bearer Token do LocalStorage
        var token = LocalStorageHelper.GetItem("token");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);



        // Adiciona Api-Key do LocalStorage
        var apiKey = LocalStorageHelper.GetItem("apiKey");
        if (!string.IsNullOrEmpty(apiKey))
            request.Headers.Add("Api-Key", apiKey);

        return await base.SendAsync(request, cancellationToken);
    }
}
