using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;

namespace StarkAid.Api.Services.CommandRouter
{
    public interface ICommandRouter
    {
        Task<CommandResult> RouteAsync(CommandRequestDto request);
    }
}
