using SerialDebugStudio.Application.Services.Checksums;
using SerialDebugStudio.Domain.Enums;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Checksums;

public sealed class ChecksumServiceCrc16GsmTests
{
    private readonly ChecksumService _service = new();

    [Fact]
    public void Calculate_Crc16Gsm_ReturnsStandardCheckValue()
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
            ChecksumAlgorithm.Crc16Gsm);

        byte[] expected =
        {
            0xCE,
            0x3C
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_Crc16Gsm_ReturnsExpectedValueForSamplePacket()
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
            ChecksumAlgorithm.Crc16Gsm);

        byte[] expected =
        {
            0xF5,
            0xC7
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Append_Crc16Gsm_AppendsHighByteThenLowByte()
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
            ChecksumAlgorithm.Crc16Gsm);

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
            0xCE,
            0x3C
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_Crc16Gsm_WithEmptyData_ReturnsFinalXorValue()
    {
        byte[] result = _service.Calculate(
            Array.Empty<byte>(),
            ChecksumAlgorithm.Crc16Gsm);

        byte[] expected =
        {
            0xFF,
            0xFF
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Append_Crc16Gsm_DoesNotModifyOriginalData()
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
            ChecksumAlgorithm.Crc16Gsm);

        Assert.Equal(originalCopy, data);
    }
}