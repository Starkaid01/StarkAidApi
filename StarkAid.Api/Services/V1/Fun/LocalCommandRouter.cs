using System;
using System.Threading.Tasks;

namespace StarkAid.Api.Services.V1.Fun
{
    public interface ILocalCommandRouter
    {
        Task<(bool Handled, string Response)> TryExecuteFunCommandAsync(Guid userId, string text);
    }

    public class LocalCommandRouter : ILocalCommandRouter
    {
        private readonly IIntentDetector _intentDetector;
        private readonly IMathService _mathService;
        private readonly IJokeService _jokeService;

        public LocalCommandRouter(
            IIntentDetector intentDetector,
            IMathService mathService,
            IJokeService jokeService)
        {
            _intentDetector = intentDetector;
            _mathService = mathService;
            _jokeService = jokeService;
        }

        public async Task<(bool Handled, string Response)> TryExecuteFunCommandAsync(Guid userId, string text)
        {
            var intent = _intentDetector.DetectIntent(text);

            switch (intent)
            {
                case FunIntent.Math:
                    var mathResult = _mathService.TryCalculate(text);
                    if (mathResult.Success)
                        return (true, mathResult.Result);
                    break;

                case FunIntent.Joke:
                    var joke = await _jokeService.GetRandomJokeAsync(userId);
                    return (true, joke);
            }

            return (false, string.Empty);
        }
    }
}
