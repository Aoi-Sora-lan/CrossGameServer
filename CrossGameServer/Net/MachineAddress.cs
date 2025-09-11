namespace CrossGameServer.Net;

public record struct MachineAddress
{
    public string GameType;
    public string GameId;
    public string MachineId;

    public MessageAddress GetMessageAddress(int messageIndex)
    {
        return new MessageAddress()
        {
            GameId = GameId,
            GameType = GameType,
            MachineId = MachineId,
            MessageIndex = messageIndex
        };
    }
}