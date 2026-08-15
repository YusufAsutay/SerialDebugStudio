namespace SerialDebugStudio.Domain.Enums;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting,
    Reconnecting,
    Faulted
}