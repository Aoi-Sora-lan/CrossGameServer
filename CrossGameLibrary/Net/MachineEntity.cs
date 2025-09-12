using CrossGameLibrary.Base;
using CrossGameLibrary.Message;

namespace CrossGameLibrary.Net;

public class MachineEntity
{
    public MachineAddress Address;
    public MessageHandler Handler;
    private int _nowMessageIndex = 0;
    private IMachineLogic _logic;

    private async Task SendMessage(MessageType type, int targetChannel, object? content = null)
    {
        var message = new Message
        {
            MessageType = type,
            SourceAddress = Address.GetMessageAddress(_nowMessageIndex++),
            TargetChannel = targetChannel,
            Content = content
        };
        await Handler.SendMessage(message);
    }
    
    public async Task SendChangeMachineNameMessage(string machineName, int channel)
    {
        await SendMessage(MessageType.ChangeMachineName, channel, new ChangeMachineNameRequest
        {
            Name = machineName
        });
    }
    public async Task SendRegisterMachineMessage()
    {
        await SendMessage(MessageType.RegisterMachine, -1);
    }
    public async Task SendRemoveMachineMessage()
    {
        await SendMessage(MessageType.RemoveMachine, -1);
    }
    public async Task SendSetChannelMessage(int channel, MachineIOType ioType)
    {
        await SendMessage(MessageType.SetChannel, channel, new SetChannelRequest()
        {
            ChannelId = channel,
            IOType = ioType
        });
    }

    public async Task SendItemRequestMessage(int channel, string itemId, int itemCount)
    {
        var package = new ItemPackage
        {
            ItemId = itemId,
            ItemCount = itemCount
        };
        await SendMessage(MessageType.ItemRequest, channel, package);
        _logic.PreSend();
    }
    public MachineEntity(MachineAddress address, BaseUdpClient client, IMachineLogic logic)
    {
        Address = address;
        _logic = logic;
        Handler = new MessageHandler(address, client, logic);
    }

    public void OnConsumeMessage(Message receivedMessage)
    {
        Handler.OnConsumeMessage(receivedMessage);
    }

    public async Task SendSetSignalMessage(int channel)
    {
        await SendMessage(MessageType.Signal, channel);
    }
}