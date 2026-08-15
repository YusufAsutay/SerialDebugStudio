using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Domain;

public sealed class DomainModelTests
{
    [Fact]
    public void SerialPortSettings_ShouldHaveExpectedDefaults()
    {
        var settings = new SerialPortSettings();

        Assert.Equal(9600, settings.BaudRate);
        Assert.Equal(SerialDataBits.Eight, settings.DataBits);
        Assert.Equal(SerialParity.None, settings.Parity);
        Assert.Equal(SerialStopBits.One, settings.StopBits);
        Assert.Equal(SerialFlowControl.None, settings.FlowControl);
    }

    [Fact]
    public void SerialPortSettings_ShouldAcceptCustomBaudRate()
    {
        var settings = new SerialPortSettings
        {
            PortName = "COM3",
            BaudRate = 250000
        };

        Assert.Equal("COM3", settings.PortName);
        Assert.Equal(250000, settings.BaudRate);
    }

    [Fact]
    public void DataTransferRecord_ShouldCopyInputData()
    {
        byte[] source = [0x41, 0x42, 0x43];

        var record = new DataTransferRecord(
            DataDirection.Received,
            TransportKind.Serial,
            source);

        source[0] = 0xFF;

        Assert.Equal(3, record.ByteCount);
        Assert.Equal((byte)0x41, record.Data.Span[0]);
    }
}