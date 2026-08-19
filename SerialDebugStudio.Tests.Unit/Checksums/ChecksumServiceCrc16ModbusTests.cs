using SerialDebugStudio.Application.Services.Checksums;
using SerialDebugStudio.Domain.Enums;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Checksums;

public sealed class ChecksumServiceCrc16ModbusTests
{
    private readonly ChecksumService _service = new();

    [Fact]
    public void Calculate_Crc16Modbus_ReturnsExpectedBytesForReadHoldingRegisters()
    {
        byte[] data =
        {
            0x01,
            0x03,
            0x00,
            0x00,
            0x00,
            0x0A
        };

        byte[] result = _service.Calculate(
            data,
            ChecksumAlgorithm.Crc16Modbus);

        byte[] expected =
        {
            0xC5,
            0xCD
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_Crc16Modbus_ReturnsExpectedBytesForSecondKnownVector()
    {
        byte[] data =
        {
            0x11,
            0x03,
            0x00,
            0x6B,
            0x00,
            0x03
        };

        byte[] result = _service.Calculate(
            data,
            ChecksumAlgorithm.Crc16Modbus);

        byte[] expected =
        {
            0x76,
            0x87
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Append_Crc16Modbus_AppendsLowByteThenHighByte()
    {
        byte[] data =
        {
            0x01,
            0x03,
            0x00,
            0x00,
            0x00,
            0x0A
        };

        byte[] result = _service.Append(
            data,
            ChecksumAlgorithm.Crc16Modbus);

        byte[] expected =
        {
            0x01,
            0x03,
            0x00,
            0x00,
            0x00,
            0x0A,
            0xC5,
            0xCD
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_Crc16Modbus_WithEmptyData_ReturnsInitialValue()
    {
        byte[] result = _service.Calculate(
            Array.Empty<byte>(),
            ChecksumAlgorithm.Crc16Modbus);

        byte[] expected =
        {
            0xFF,
            0xFF
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Append_Crc16Modbus_DoesNotModifyOriginalArray()
    {
        byte[] data =
        {
            0x01,
            0x03,
            0x00,
            0x00,
            0x00,
            0x0A
        };

        byte[] originalCopy =
            data.ToArray();

        _service.Append(
            data,
            ChecksumAlgorithm.Crc16Modbus);

        Assert.Equal(originalCopy, data);
    }
}