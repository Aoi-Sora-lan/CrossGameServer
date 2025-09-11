using CrossGameServer.Requests;
using Serilog;

namespace CrossGameServer.Net;

public class MessageHandler
{
    private BaseUdpServer _server;
    private Channel[] _channels;
    private Dictionary<MachineAddress, int> _machineChannelMapper = new();

    public MessageHandler(int channelCount, BaseUdpServer server)
    {
        _server = server;
        _channels = new Channel[channelCount];
        for (var i = 0; i < channelCount; i++)
        {
            _channels[i] = new Channel(_server);
        }
    }
    public async void OnConsumeMessage(Message message)
    {
        Log.Debug("{msg}",message);
        switch (message.MessageType)
        {
            case MessageType.SetChannel:
                await HandleSetChannel(message);
                break;
            default:
                _channels[message.TargetChannel].OnConsumeMessage(message);
                break;
        }
    }
    private async Task HandleSetChannel(Message message)
    {
        var request = message.GetContent<SetChannelRequest>();
        var channel = _channels[request.ChannelId];
        var sourceMachineAddress = message.SourceAddress.GetMachineAddress();
        if (_machineChannelMapper.TryGetValue(sourceMachineAddress, out var oldChannelIndex)&&oldChannelIndex!=request.ChannelId)
        {
            var oldChannel = _channels[oldChannelIndex];
            oldChannel.Remove(sourceMachineAddress);
        }
        var response = channel.Register(message ,request.IOType);
        _machineChannelMapper[sourceMachineAddress] = request.ChannelId;
        await SendMessage(response, message.SourceAddress);
    }

    public void RemoveMachine(MachineAddress machineAddress)
    {
        _channels[_machineChannelMapper[machineAddress]].Remove(machineAddress);
        _machineChannelMapper.Remove(machineAddress);
    }
    private async Task SendMessage(Message message, MessageAddress targetMessageAddress)
    { 
        await _server.SendMessage(message, targetMessageAddress);
    }
}