namespace CrossGameServer.Net;

public record struct MessageAddress
{
    public string GameType;
    public string GameId;
    public string MachineId;
    public int MessageIndex;

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