using SerialDebugStudio.Application.Services.Checksums;
using SerialDebugStudio.Domain.Enums;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Checksums;

public sealed class ChecksumServiceCrc8OpenSafetyTests
{
    private readonly ChecksumService _service = new();

    [Fact]
    public void Calculate_Crc8OpenSafety_ReturnsStandardCheckValue()
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
            ChecksumAlgorithm.Crc8OpenSafety);

        Assert.Equal(
            new byte[] { 0x3E },
            result);
    }

    [Fact]
    public void Calculate_Crc8OpenSafety_ReturnsExpectedProtocolVector()
    {
        byte[] data =
        {
            0x23,
            0xC8,
            0x08,
            0x34,
            0x11,
            0x22,
            0x33,
            0x44,
            0x55,
            0x66,
            0x77,
            0x88
        };

        byte[] result = _service.Calculate(
            data,
            ChecksumAlgorithm.Crc8OpenSafety);

        Assert.Equal(
            new byte[] { 0x3C },
            result);
    }

    [Fact]
    public void Append_Crc8OpenSafety_AppendsCheckByte()
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
            ChecksumAlgorithm.Crc8OpenSafety);

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
            0x3E
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_Crc8OpenSafety_WithEmptyData_ReturnsInitialValue()
    {
        byte[] result = _service.Calculate(
            Array.Empty<byte>(),
            ChecksumAlgorithm.Crc8OpenSafety);

        Assert.Equal(
            new byte[] { 0x00 },
            result);
    }
}   