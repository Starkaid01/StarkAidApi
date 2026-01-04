using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;

namespace StarkAid.Api.Services.CommandRouter
{
    public interface ICommandHandler
    {
        bool CanHandle(CommandRequestDto request);
        Task<CommandResult> ExecuteAsync(CommandRequestDto request);
    }
}
