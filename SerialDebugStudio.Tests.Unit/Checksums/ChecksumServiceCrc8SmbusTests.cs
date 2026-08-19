using SerialDebugStudio.Application.Services.Checksums;
using SerialDebugStudio.Domain.Enums;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Checksums;

public sealed class ChecksumServiceCrc8SmbusTests
{
    private readonly ChecksumService _service = new();

    [Fact]
    public void Calculate_Crc8Smbus_ReturnsStandardCheckValue()
    {
        byte[] data =
        {
            0x31,
            0x32,
            0x33,
            0x34,
            0x35,
            0x36,
            0x37,
            0x38,
            0x39
        };

        byte[] result = _service.Calculate(
            data,
            ChecksumAlgorithm.Crc8Smbus);

        Assert.Equal(
            new byte[] { 0xF4 },
            result);
    }

    [Fact]
    public void Calculate_Crc8Smbus_ReturnsExpectedValueForSamplePacket()
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
            ChecksumAlgorithm.Crc8Smbus);

        Assert.Equal(
            new byte[] { 0xB9 },
            result);
    }

    [Fact]
    public void Append_Crc8Smbus_AppendsCheckByte()
    {
        byte[] data =
        {
            0x31,
            0x32,
            0x33,
            0x34,
            0x35,
            0x36,
            0x37,
            0x38,
            0x39
        };

        byte[] result = _service.Append(
            data,
            ChecksumAlgorithm.Crc8Smbus);

        byte[] expected =
        {
            0x31,
            0x32,
            0x33,
            0x34,
            0x35,
            0x36,
            0x37,
            0x38,
            0x39,
            0xF4
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_Crc8Smbus_WithEmptyData_ReturnsInitialValue()
    {
        byte[] result = _service.Calculate(
            Array.Empty<byte>(),
            ChecksumAlgorithm.Crc8Smbus);

        Assert.Equal(
            new byte[] { 0x00 },
            result);
    }
}