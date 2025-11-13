namespace StarkAid.Api.Services.Devices;
using StarkAid.Api.Entities;
using System.Threading.Channels;

public class AgendamentoQueueService
{
    private readonly Channel<Agendamento> _channel;

    public AgendamentoQueueService()
    {
        _channel = Channel.CreateUnbounded<Agendamento>();
    }

    public ChannelReader<Agendamento> Reader => _channel.Reader;

    public async Task EnfileirarAsync(Agendamento agendamento)
    {
        await _channel.Writer.WriteAsync(agendamento);
    }
}
