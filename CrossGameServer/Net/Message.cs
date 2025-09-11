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