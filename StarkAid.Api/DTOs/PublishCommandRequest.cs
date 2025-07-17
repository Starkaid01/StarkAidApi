namespace StarkAid.Api.DTOs
{
    public enum CommandType
    {
        ligar,
        desligar
    }

    public class PublishCommandRequest
    {
        public Guid DeviceId { get; set; }
        public CommandType Command { get; set; }
    }

}