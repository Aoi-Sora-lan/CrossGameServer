namespace CrossGameServer.Net;

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