using CrossGameServer.Requests;
using CrossGameServer.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CrossGameServer.Net;

public partial class Channel(BaseUdpServer server)
{
    private readonly List<MachineEntity> _machines = []; 
    private readonly MessageBuilder _messageBuilder = new();
    private readonly Dictionary<MessageAddress, TaskCompletionSource<Message>> _waitForResponseMessages = new();

    private MachineEntity? GetOutputMachine()
    {
        var outputMachine = _machines.FirstOrDefault(machine => machine.IOType == MachineIOType.Output);
        if(outputMachine.MachineAddress.MachineId == null) return null;
        return outputMachine;
    }
    private Message? ModifyItemRequest(Message message, string sourceGameType, string targetGameType)
    {
        var request = message.GetContent<ItemPackage>();
        var result = ItemTransferHelper.Instance.Transfer(request.ItemId, request.ItemCount, sourceGameType, targetGameType);
        if (result.TransferPackage.ItemCount <= 0) return null;
        return _messageBuilder.Copy(message).SetContent(result.TransferPackage).Build();
    }
    private Message ModifyResponse(Message message, string sourceGameType, string targetGameType)
    {
        var response = message.GetContent<ItemResponse>();
        var result = ItemTransferHelper.Instance.Transfer(response.ItemId, response.ItemCount, sourceGameType, targetGameType);
        response = response with { ItemId = result.TransferPackage.ItemId, ItemCount = result.TransferPackage.ItemCount };
        return _messageBuilder.Copy(message).SetContent(response).Build();
    }
    public async Task<Message> WaitingForItemResponse(Message request, MessageAddress outputMessageAddress)
    {
        var taskSource = new TaskCompletionSource<Message>();
        _waitForResponseMessages.Add(request.SourceAddress, taskSource);
        await SendMessage(request, outputMessageAddress);
        return await taskSource.Task;
    }
    private async Task SendMessage(Message message, MessageAddress targetAddress)
    { 
        await server.SendMessage(message, targetAddress);
    }
    private async Task SendMessage(Message message)
    { 
        await server.SendMessage(message, message.TargetAddress!.Value);
    }

    private int GetMachineIndex(MachineAddress address)
    {
        var index = -1;
        for (var i = 0; i < _machines.Count; i++)
        {
            if (_machines[i].MachineAddress != address) continue;
            index = i;
            break;
        }
        return index;
    }
    
    public Message Register(Message message, MachineIOType requestIOType)
    {
        var index = GetMachineIndex(message.SourceAddress.GetMachineAddress());
        if (index != -1)
            _machines[index] = _machines[index] with { IOType = requestIOType };
        else 
            _machines.Add(new MachineEntity(message.SourceAddress, requestIOType, "DefaultMachine"));
        return _messageBuilder.Copy(message)
            .SetType(MessageType.Response).Build();
    }

    public void Remove(MachineAddress address)
    {
        var index = GetMachineIndex(address);
        _machines.RemoveAt(index);
    }

    public List<MachineEntity> GetMachines()
    {
        return _machines;
    }
}