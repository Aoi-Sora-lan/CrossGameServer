using CrossGameServer.Requests;
using CrossGameServer.Utils;
using Serilog;

namespace CrossGameServer.Net;


public partial class Channel
{
    private async Task HandleItemRequest(Message request)
    {
        var output = GetOutputMachine();
        if (!output.HasValue)
        {
            await SendFailureItemResponse(request, "no output");
            return;
        }
        var requestGameType = request.SourceAddress.GameType;
        var outputMessageAddress = output.Value.MachineAddress.GetMessageAddress(request.SourceAddress.MessageIndex);
        var responseGameType = outputMessageAddress.GameType;
        var requestMessage = ModifyItemRequest(request, requestGameType, responseGameType);
        request.TargetAddress = outputMessageAddress;
        if(requestMessage is not null)
        {
            var response = await WaitingForItemResponse(requestMessage.Value, outputMessageAddress);
            _waitForResponseMessages.Remove(requestMessage.Value.SourceAddress);
            var responseMessage = ModifyResponse(response, responseGameType, requestGameType);
            await SendMessage(responseMessage);
        }
        else
            await SendFailureItemResponse(request, "item not found");
    }

    private async Task SendFailureItemResponse(Message request, string reason)
    {
        var responseMessage = _messageBuilder.Copy(request)
            .ReverseAddress()
            .SetType(MessageType.ItemResponse)
            .SetContent(new ItemResponse()
            {
                IsSuccess = false,
                Reason = reason
            }).Build();
        await SendMessage(responseMessage);
    }


    private Task HandleItemResponse(Message response)
    {
        if (_waitForResponseMessages.TryGetValue(response.TargetAddress!.Value, out var requestMessage))
        {
            requestMessage.SetResult(response);
        }
        return Task.CompletedTask;
    }
    private async Task HandleTransfer(Message transfer)
    {
        var requestGameType = transfer.SourceAddress.GameType;
        var outputMessageAddress = transfer.TargetAddress;
        var responseGameType = outputMessageAddress.Value.GameType;
        var content = transfer.GetContent<ItemPackage?>();
        var contentValue = content.Value;
        var result = ItemTransferHelper.Instance.Transfer(
            contentValue.ItemId, contentValue.ItemCount, requestGameType, responseGameType);
        transfer = transfer with { Content = result.TransferPackage };
        await SendMessage(transfer);
    }

    private async Task HandleSignal(Message message)
    {
        var outputMachine = GetOutputMachine();
        if(!outputMachine.HasValue) return;
        var targetAddress = outputMachine.Value.MachineAddress;
        await SendMessage(message, targetAddress.GetMessageAddress(message.SourceAddress.MessageIndex));
    }

    public async void OnConsumeMessage(Message message)
    {
        switch (message.MessageType)
        {
            case MessageType.ItemRequest:
                await HandleItemRequest(message);
                break;
            case MessageType.ItemResponse:
                await HandleItemResponse(message);
                break;
            case MessageType.Transfer:
                await HandleTransfer(message);
                break;
            case MessageType.Signal:
                await HandleSignal(message);
                break;
            case MessageType.ChangeMachineName:
                await HandleChangeMachineName(message);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task HandleChangeMachineName(Message message)
    {
        var content = message.GetContent<ChangeMachineNameRequest>();
        var index = _machines.FindIndex(machine => machine.MachineAddress == message.SourceAddress.GetMachineAddress());
        if (index >= 0)
        {
            var target = _machines[index];
            _machines[index] = target with
            {
                Name = content!.Name
            };
        }
    }
}