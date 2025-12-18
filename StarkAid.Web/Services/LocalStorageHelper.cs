using Microsoft.JSInterop;

namespace StarkAid.Web.Services
{
    public static class LocalStorageHelper
    {
        private static IJSRuntime? _js;

        public static void Configure(IJSRuntime js)
        {
            _js = js;
        }

        public static string? GetItem(string key)
        {
            if (_js == null) throw new InvalidOperationException("LocalStorageHelper não foi configurado com IJSRuntime.");

            var task = _js.InvokeAsync<string>("localStorage.getItem", key);
            task.GetAwaiter();
            return task.Result;
        }

        public static void SetItem(string key, string value)
        {
            if (_js == null) throw new InvalidOperationException("LocalStorageHelper não foi configurado com IJSRuntime.");

            var task = _js.InvokeVoidAsync("localStorage.setItem", key, value);
            task.AsTask().Wait();
        }

        public static void RemoveItem(string key)
        {
            if (_js == null) throw new InvalidOperationException("LocalStorageHelper não foi configurado com IJSRuntime.");

            var task = _js.InvokeVoidAsync("localStorage.removeItem", key);
            task.AsTask().Wait();
        }
    }
}
