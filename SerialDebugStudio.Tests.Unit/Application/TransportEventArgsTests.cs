using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Application;

public sealed class TransportEventArgsTests
{
    [Fact]
    public void DataReceivedEventArgs_ShouldCopyReceivedData()
    {
        byte[] source = [0x41, 0x42, 0x43];

        var timestamp = new DateTimeOffset(
            2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

        var eventArgs = new TransportDataReceivedEventArgs(
            TransportKind.Serial,
            source,
            timestamp);

        source[0] = 0xFF;

        Assert.Equal(3, eventArgs.ByteCount);
        Assert.Equal((byte)0x41, eventArgs.Data.Span[0]);
        Assert.Equal(DataDirection.Received, eventArgs.Direction);
        Assert.Equal(TransportKind.Serial, eventArgs.TransportKind);
        Assert.Equal(timestamp, eventArgs.TimestampUtc);
    }

    [Fact]
    public void ConnectionStateChangedEventArgs_ShouldContainStates()
    {
        var connectionInfo = new ConnectionInfo(
            TransportKind.Serial,
            ConnectionState.Connected,
            "COM3",
            DateTimeOffset.UtcNow);

        var eventArgs =
            new TransportConnectionStateChangedEventArgs(
                ConnectionState.Connecting,
                connectionInfo);

        Assert.Equal(
            ConnectionState.Connecting,
            eventArgs.PreviousState);

        Assert.Equal(
            ConnectionState.Connected,
            eventArgs.CurrentState);

        Assert.Equal(
            "COM3",
            eventArgs.ConnectionInfo.Endpoint);
    }
}