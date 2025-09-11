using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
            var address = receivedMessage.SourceAddress.GetMachineAddress();
            switch (receivedMessage.MessageType)
            {
                case MessageType.RegisterMachine:
                    HandleRegisterMachine(address, result.RemoteEndPoint);
                    continue;
                case MessageType.RemoveMachine:
                    HandleRemoveMachine(address);
                    continue;
            }
            _messageHandler.OnConsumeMessage(receivedMessage);
        }
    }

    private void HandleRemoveMachine(MachineAddress machineAddress)
    {
        _machineMapper.Remove(machineAddress);
        _messageHandler.RemoveMachine(machineAddress);
    }

    private void HandleRegisterMachine(MachineAddress machineAddress, IPEndPoint endPoint)
    {
        _machineMapper.TryAdd(machineAddress, endPoint);
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
}