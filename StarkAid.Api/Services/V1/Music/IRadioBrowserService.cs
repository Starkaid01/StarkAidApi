using StarkAid.Api.DTOs.V1.Music;

namespace StarkAid.Api.Services.V1.Music
{
    public interface IRadioBrowserService
    {
        Task<List<MusicStationStation>> SearchAsync(string? name = null, string? tag = null, string? country = null);
        Task<MusicStationStation?> ResolveBestRadioAsync(string query, string? category = null);
    }
}
