using System;
using System.Threading.Tasks;

namespace StarkAid.Web.Services
{
    public class ConfirmService
    {
        private TaskCompletionSource<bool>? _tcs;
        public event Action<string, string>? OnShow;

        public Task<bool> ConfirmAsync(string title, string message)
        {
            _tcs = new TaskCompletionSource<bool>();
            OnShow?.Invoke(title, message);
            return _tcs.Task;
        }

        public void SetResult(bool result)
        {
            _tcs?.SetResult(result);
        }
    }
}
