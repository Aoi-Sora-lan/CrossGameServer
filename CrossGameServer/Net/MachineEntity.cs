namespace CrossGameServer.Net;

public record struct MachineEntity
{
    public MachineAddress MachineAddress;
    public MachineIOType IOType;
    public string Name;

    public MachineEntity(MessageAddress messageAddress, MachineIOType ioType, string name)
    {
        IOType = ioType;
        Name = name;
        MachineAddress = messageAddress.GetMachineAddress();
    }

    public bool Equals(MachineEntity? other)
    {
        return other != null && other.Value.MachineAddress.Equals(MachineAddress);
    }
}