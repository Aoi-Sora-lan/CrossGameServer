#nullable enable
using Newtonsoft.Json.Linq;

namespace CrossGameLibrary.Net;

public struct Message
{
    public MessageType MessageType;
    public MessageAddress SourceAddress;
    //服务器转发附加
    public MessageAddress? TargetAddress = null;
    public int TargetChannel;
    public object? Content;
    public T? GetContent<T>()
    {
        return Content == null ? default : ((JObject)Content!).ToObject<T>();
    }
    public Message()
    {
        MessageType = MessageType.None;
    }
}

public enum MessageType
{
    None,
    ItemRequest,
    ItemResponse,
    Response,
    Transfer,
    Signal,
    SetChannel,
    RegisterMachine,
    RemoveMachine,
    ChangeMachineName,
}
public record struct MessageGame
{
    public string GameType;
    public string GameId;
}