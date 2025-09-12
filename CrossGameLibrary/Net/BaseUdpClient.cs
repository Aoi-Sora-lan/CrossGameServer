using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CrossGameLibrary.Base;
using Newtonsoft.Json;
using Serilog;

namespace CrossGameLibrary.Net;

public class BaseUdpClient : IDisposable
{
    private readonly UdpClient _udpClient;
    private static IPEndPoint _remoteEndPoint;
    private readonly ConcurrentQueue<UdpReceiveResult> _receiveResults = new();
    private readonly List<MachineEntity> _machineEntities = new();
    private readonly SemaphoreSlim _messageSignal = new(1);
    private bool _isRunning;
    public static bool IsOnline;
    public BaseUdpClient(int port, string remoteIp, int remotePort)
    {
        _remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
        _udpClient = new UdpClient(port);
    }
    
    public void Start()
    {
        _isRunning = true;
        Task.Run(ReceiveMessages);
        Task.Run(HandleMessages);
    }

    public MachineEntity Register(MachineAddress machineAddress, IMachineLogic logic)
    {
        var entity = new MachineEntity(machineAddress, this, logic);
        _machineEntities.Add(entity);
        return entity;
    }

    public async Task RemoveMachine(MachineEntity machineEntity)
    {
        await machineEntity.SendRemoveMachineMessage();
        _machineEntities.Remove(machineEntity);
    }
    
    public async Task RemoveMachines()
    {
        foreach (var machineEntity in _machineEntities)
        {
            await machineEntity.SendRemoveMachineMessage();
        }
        _machineEntities.Clear();
    }
    
    private async Task HandleMessages()
    {
        while (_isRunning)
        {
            await _messageSignal.WaitAsync();
            if(!_receiveResults.TryDequeue(out var result)) continue;
            var receivedMessageBytes = Encoding.UTF8.GetString(result.Buffer);
            var receivedMessage = JsonConvert.DeserializeObject<Message>(receivedMessageBytes);
            _machineEntities.First(entity=>entity.Address.MachineId == receivedMessage.TargetAddress.Value.MachineId).OnConsumeMessage(receivedMessage);
        }
    }


    public async Task SendMessage(Message message)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        await _udpClient.SendAsync(bytes, _remoteEndPoint);
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

    public static bool JudgeOnline()
    {
        if (!IsOnline)
        {
            Log.Error("未连接到服务器:{ip}:{port}，请查看服务器是否打开", _remoteEndPoint.Address, _remoteEndPoint.Port);
            return false;
        }
        return true;
    }

    public void Dispose()
    {
        IsOnline = false;
        _udpClient.Dispose();
    }

}