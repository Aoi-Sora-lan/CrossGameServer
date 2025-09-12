namespace CrossGameLibrary.Net;

public class MessageBuilder
{
    private Message _message;

    public MessageBuilder SetType(MessageType type)
    {
        _message.MessageType = type;
        return this;
    }

    public MessageBuilder ReverseAddress()
    {
        var temp = _message.SourceAddress;
        _message.SourceAddress = _message.TargetAddress.Value;
        _message.TargetAddress = temp;
        return this;
    }
    public MessageBuilder Copy(Message message)
    {
        _message = message;
        return this;
    }

    public MessageBuilder SetContent(object content)
    {
        _message.Content = content;
        return this;
    }

    public Message Build()
    {
        return _message;
    }
}