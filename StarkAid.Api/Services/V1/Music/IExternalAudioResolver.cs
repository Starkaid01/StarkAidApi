using System.Threading.Tasks;
using StarkAid.Api.DTOs.V1.Music;

namespace StarkAid.Api.Services.V1.Music
{
    public interface IExternalAudioResolver
    {
        Task<ExternalAudioStreamResult?> GetAudioStreamUrlAsync(string externalId);
    }
}
