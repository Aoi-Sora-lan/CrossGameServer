namespace CrossGameLibrary.Net;

public record struct MessageAddress
{
    public string GameType;
    public string GameId;
    public string MachineId;
    public int MessageIndex;
    public override string ToString()
    {
        return $"{GameType}/{GameId}/{MachineId}/{MessageIndex}";
    }
    public MachineAddress GetMachineAddress()
    {
        return new MachineAddress()
        {
            GameId = GameId,
            GameType = GameType,
            MachineId = MachineId
        };
    }
}

public record struct MachineAddress
{
    public string GameType;
    public string GameId;
    public string MachineId;
    public override string ToString()
    {
        return $"{GameType}/{GameId}/{MachineId}";
    }

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