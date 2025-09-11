using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CrossGameServer.Requests;
using Newtonsoft.Json;
using Serilog;

namespace CrossGameServer.Net;

public class BaseUdpServer : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly ConcurrentQueue<UdpReceiveResult> _receiveResults = new();
    private readonly MessageHandler _messageHandler;
    private readonly Dictionary<MachineAddress, IPEndPoint> _machineMapper = new();
    private readonly SemaphoreSlim _messageSignal = new(0);
    private List<MessageLog> _messageLogs = new();
    private bool _isRunning;
    public BaseUdpServer(int port, int channelCount)
    {
        _messageHandler = new MessageHandler(channelCount, this);
        _udpClient = new UdpClient(port);
    }
    public void Start()
    {
        _isRunning = true;
        Task.Run(ReceiveMessages);
        Task.Run(HandleMessages);
    }
    public async Task SendMessage(Message message, MessageAddress address)
    {
        var endPoint = _machineMapper[address.GetMachineAddress()];
        message.TargetAddress = address;
        SaveLogMessage(message, true);
        Log.Debug("向{addr}发送:{msg}",endPoint, JsonConvert.SerializeObject(message));
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        await _udpClient.SendAsync(bytes, endPoint);
    }
    private async Task HandleMessages()
    {
        while (_isRunning)
        {
            await _messageSignal.WaitAsync();
            if(!_receiveResults.TryDequeue(out var result)) continue;
            var receivedMessageBytes = Encoding.UTF8.GetString(result.Buffer);
            var receivedMessage = JsonConvert.DeserializeObject<Message>(receivedMessageBytes);
            SaveLogMessage(receivedMessage, false);
            var address = receivedMessage.SourceAddress.GetMachineAddress();
            switch (receivedMessage.MessageType)
            {
                case MessageType.RegisterMachine:
                    HandleRegisterMachine(receivedMessage, address, result.RemoteEndPoint);
                    continue;
                case MessageType.RemoveMachine:
                    await HandleRemoveMachine(receivedMessage, address);
                    continue;
            }
            _messageHandler.OnConsumeMessage(receivedMessage);
        }
    }

    private async Task HandleRemoveMachine(Message message, MachineAddress machineAddress)
    {
        _machineMapper.Remove(machineAddress);
        _messageHandler.RemoveMachine(machineAddress);
       
    }

    private async Task HandleRegisterMachine(Message message, MachineAddress machineAddress, IPEndPoint endPoint)
    {
        _machineMapper.TryAdd(machineAddress, endPoint);
        await SendMessage(new Message()
        {
            SourceAddress = message.SourceAddress,
            MessageType = MessageType.Response,
            Content = Response.Success
        }, message.SourceAddress);
    }
    private async Task ReceiveMessages()
    {
        while (_isRunning)
        {
            var result = await _udpClient.ReceiveAsync();
            _receiveResults.Enqueue(result);
            _messageSignal.Release();
        }
    }
    public void Dispose()
    {
        _isRunning = false;
        _udpClient.Dispose();
    }

    public Dictionary<MachineAddress, IPEndPoint> GetMachineMapper()
    {
        return _machineMapper;
    }

    public void SaveLogMessage(Message message, bool isFromServer)
    {
        _messageLogs.Add(new MessageLog(message, isFromServer));
    }
    
    public List<MessageLog> GetMessages()
    {
        return _messageLogs;
    }

    public List<List<MachineEntity>> GetChannels()
    {
        return _messageHandler.GetChannels();
    }
}