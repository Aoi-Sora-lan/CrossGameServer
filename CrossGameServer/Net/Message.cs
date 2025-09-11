using Newtonsoft.Json.Linq;

namespace CrossGameServer.Net;

public record struct Message()
{
    public MessageType MessageType = MessageType.None;
    public MessageAddress SourceAddress;
    //服务器转发附加
    public MessageAddress? TargetAddress = null;
    public int TargetChannel;
    public object? Content;

    public T? GetContent<T>()
    {
        return Content == null ? default : ((JObject)Content!).ToObject<T>();
    }
}

public record struct MessageLog
{
    public MessageType MessageType = MessageType.None;
    public MessageAddress SourceAddress;
    public MessageAddress? TargetAddress = null;
    public int TargetChannel;
    public object? Content;
    public DateTime Timestamp;
    public bool IsFromServer;

    public MessageLog(Message message, bool fromServer)
    {
        MessageType = message.MessageType;
        SourceAddress = message.SourceAddress;
        TargetAddress = message.TargetAddress;
        TargetChannel = message.TargetChannel;
        Content = message.Content;
        Timestamp = DateTime.Now;
        IsFromServer = fromServer;
    }
}