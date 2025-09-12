using CrossGameLibrary.Base;
using CrossGameLibrary.Message;
using Serilog;

namespace CrossGameLibrary.Net;

public class MessageHandler
{
    private MachineAddress _hostAddress;
    private IMachineLogic _logic;
    private BaseUdpClient _client;
    private Dictionary<int, TaskCompletionSource<Message>> _waitForResponseMessages = new();
    private MessageBuilder _messageBuilder = new();

    public MessageHandler(MachineAddress address, BaseUdpClient client, IMachineLogic logic)
    {
        _hostAddress = address;
        _client = client;
        _logic = logic;
    }

    public async void OnConsumeMessage(Message message)
    {
        LogReceiveMessage(message);
        switch (message.MessageType)
        {
            case MessageType.None:
                break;
            case MessageType.Response:
            {
                await HandleResponse(message);
                break;
            }
            case MessageType.ItemRequest:
            {
                await HandleItemRequest(message);
                break;
            }
            case MessageType.ItemResponse:
            {
                await HandleItemResponse(message);
                break;
            }
            case MessageType.Transfer:
            {
                await HandleTransfer(message);
                break;
            }
            case MessageType.Signal:
                await HandleSignal(message);
                break;
            case MessageType.RegisterMachine:
                await HandleRegisterMachine(message);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task HandleSignal(Message message)
    {
        _logic.OnSignal();
    }

    private async Task HandleTransfer(Message message)
    {
        var content = message.GetContent<ItemPackage?>();
        if(content == null) return;
        var contentValue = content.Value;
        _logic.GenerateItem(contentValue);
    }

    private async Task HandleItemResponse(Message itemResponse)
    {
        var content = itemResponse.GetContent<ItemResponse?>();
        if(!content.HasValue) return;
        var contentValue = content.Value;
        LogItemResponseMessage(contentValue);
        if (!contentValue.IsSuccess)
        {
            _logic.SendFailure();
            return;
        }
        _logic.SendSuccess(contentValue);
        await SendMessage(_messageBuilder
            .Copy(itemResponse)
            .ReverseAddress()
            .SetType(MessageType.Transfer)
            .SetContent( new ItemPackage()
            {
                ItemId = contentValue.ItemId!,
                ItemCount = contentValue.ItemCount
            })
            .Build());
    }

    private async Task HandleResponse(Message response)
    {
        if (response.GetContent<Response>().IsSuccess)
        {
            BaseUdpClient.IsOnline = true;
        }
    }

    public async Task SendRegister()
    {
    }
    
    public async Task SendMessage(Message message)
    {
        LogSendMessage(message);
        if(message.Content is ItemPackage pkg) LogItemPackage(pkg);
        if(message.Content is ItemResponse response) LogItemResponseMessage(response);
        await _client.SendMessage(message);
    }

    private string GenerateMachineId()
    {
        return Guid.NewGuid().ToString();
    }
    
    private async Task HandleRegisterMachine(Message register)
    {
    }



    
    private async Task HandleItemRequest(Message itemRequest)
    {
        var content = itemRequest.GetContent<ItemPackage>();
        LogItemPackage(content);
        var canTransfer = _logic.CanTransfer(content.ItemId, content.ItemCount);
        if (canTransfer)
        {
            var maxNeed = _logic.GetMaxNeedCount();
            var count = Math.Min(maxNeed, content.ItemCount);
            await SendMessage(_messageBuilder
                .Copy(itemRequest)
                .ReverseAddress()
                .SetType(MessageType.ItemResponse)
                .SetContent(new ItemResponse
                {
                    IsSuccess = true,
                    ItemId = content.ItemId,
                    ItemCount = count,
                }).Build());
        }
        else
        {
            await SendMessage(_messageBuilder
                .Copy(itemRequest)
                .ReverseAddress()
                .SetType(MessageType.ItemResponse)
                .SetContent(new ItemResponse
                {
                    IsSuccess = false,
                    Reason = "output blocked"
                }).Build());
        }
    }

    public async Task<Message> WaitingFor(Message request)
    {
        var taskSource = new TaskCompletionSource<Message>();
        //_waitForResponseMessages.Add(request.MessageId, taskSource);
        return await taskSource.Task;
    }

    #region Log

    public void LogReceiveMessage(Message message)
    {
        Log.Debug($"[{_hostAddress.MachineId}]收到了来自{message.SourceAddress}的{message.MessageType}消息");
    }

    public void LogItemPackage(ItemPackage package)
    {
        Log.Debug($"[{_hostAddress.MachineId}]消息内容为：{package.ItemCount}个{package.ItemId}物品");
    }

    public void LogItemResponseMessage(ItemResponse response)
    {
        var result = response.IsSuccess;
        if (result)
        {
            Log.Debug($"[{_hostAddress.MachineId}]消息内容为：成功！将转换{response.ItemCount}个{response.ItemId}物品");
        }
        else
        {
            Log.Debug($"[{_hostAddress.MachineId}]消息内容为：失败！原因是:{response.Reason}");
        }
       
    }
    public void LogSendMessage(Message message)
    {
        Log.Debug($"[{_hostAddress.MachineId}]发送了去往{message.TargetChannel}号频道的{message.MessageType}消息");
    }
    #endregion
}