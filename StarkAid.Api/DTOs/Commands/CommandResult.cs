namespace StarkAid.Api.DTOs.Commands
{
    public sealed class CommandResult
    {
        public bool IsSuccess { get; private set; }
        public string Message { get; private set; } = string.Empty;

        public static CommandResult Success(string msg) =>
            new() { IsSuccess = true, Message = msg };

        public static CommandResult Fail(string msg) =>
            new() { IsSuccess = false, Message = msg };
    }
}
