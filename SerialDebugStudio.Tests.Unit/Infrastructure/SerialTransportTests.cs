using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;
using SerialDebugStudio.Infrastructure.Transports.Serial;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Infrastructure;

public sealed class SerialTransportTests
{
    [Fact]
    public async Task NewTransport_ShouldBeClosed()
    {
        var settings = CreateSettings();

        await using var transport =
            new SerialTransport(settings);

        Assert.Equal(
            TransportKind.Serial,
            transport.Kind);

        Assert.False(transport.IsOpen);

        Assert.Equal(
            ConnectionState.Disconnected,
            transport.ConnectionInfo.State);

        Assert.Equal(
            "COM_TEST",
            transport.ConnectionInfo.Endpoint);
    }

    [Fact]
    public async Task SendAsync_WhenPortIsClosed_ShouldThrow()
    {
        var settings = CreateSettings();

        await using var transport =
            new SerialTransport(settings);

        byte[] data = [0x01, 0x02, 0x03];

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.SendAsync(data));
    }

    [Fact]
    public void Constructor_WhenPortNameIsEmpty_ShouldThrow()
    {
        var settings = new SerialPortSettings
        {
            PortName = string.Empty
        };

        Assert.Throws<ArgumentException>(
            () => new SerialTransport(settings));
    }

    private static SerialPortSettings CreateSettings()
    {
        return new SerialPortSettings
        {
            PortName = "COM_TEST",
            BaudRate = 115200,
            DataBits = SerialDataBits.Eight,
            Parity = SerialParity.None,
            StopBits = SerialStopBits.One,
            FlowControl = SerialFlowControl.None
        };
    }
}