using CrossGameServer.Net;
using Newtonsoft.Json;

namespace CrossGameServer.Requests;

public struct SetChannelRequest
{
    public MachineIOType IOType;
    public int ChannelId;
}