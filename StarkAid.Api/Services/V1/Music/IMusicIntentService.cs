using StarkAid.Api.DTOs.V1.Music;

namespace StarkAid.Api.Services.V1.Music
{
    public interface IMusicIntentService
    {
        Task<MusicResolveResponse> ResolveIntentAsync(string text);
    }
}
