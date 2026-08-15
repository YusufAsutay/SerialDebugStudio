using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Application.Abstractions.Transports;

public sealed class TransportConnectionStateChangedEventArgs : EventArgs
{
    public TransportConnectionStateChangedEventArgs(
        ConnectionState previousState,
        ConnectionInfo connectionInfo,
        DateTimeOffset? timestampUtc = null)
    {
        PreviousState = previousState;
        ConnectionInfo = connectionInfo;
        TimestampUtc = timestampUtc ?? DateTimeOffset.UtcNow;
    }

    public ConnectionState PreviousState { get; }

    public ConnectionState CurrentState =>
        ConnectionInfo.State;

    public ConnectionInfo ConnectionInfo { get; }

    public DateTimeOffset TimestampUtc { get; }
}