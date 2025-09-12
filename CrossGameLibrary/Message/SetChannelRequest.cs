using CrossGameLibrary.Net;

namespace CrossGameLibrary.Message;

public struct SetChannelRequest
{
    public MachineIOType IOType;
    public int ChannelId;
}