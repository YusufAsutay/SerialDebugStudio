using SerialDebugStudio.Application.Services.Checksums;
using SerialDebugStudio.Domain.Enums;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Checksums;

public sealed class ChecksumServiceSum8Tests
{
    private readonly ChecksumService _service = new();

    [Fact]
    public void Calculate_None_ReturnsEmptyArray()
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
            ChecksumAlgorithm.None);

        Assert.Empty(result);
    }

    [Fact]
    public void Append_None_ReturnsUnchangedData()
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
            ChecksumAlgorithm.None);

        Assert.Equal(data, result);
        Assert.NotSame(data, result);
    }

    [Fact]
    public void Calculate_Sum8_ReturnsExpectedChecksum()
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
            ChecksumAlgorithm.Sum8);

        Assert.Equal(
            new byte[] { 0x0E },
            result);
    }

    [Fact]
    public void Append_Sum8_AppendsChecksumToData()
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
            ChecksumAlgorithm.Sum8);

        byte[] expected =
        {
            0x01,
            0x03,
            0x00,
            0x00,
            0x00,
            0x0A,
            0x0E
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_Sum8_WhenSumOverflows_WrapsToEightBits()
    {
        byte[] data =
        {
            0xFF,
            0x02
        };

        byte[] result = _service.Calculate(
            data,
            ChecksumAlgorithm.Sum8);

        Assert.Equal(
            new byte[] { 0x01 },
            result);
    }

    [Fact]
    public void Calculate_Sum8_WithEmptyData_ReturnsZero()
    {
        byte[] result = _service.Calculate(
            Array.Empty<byte>(),
            ChecksumAlgorithm.Sum8);

        Assert.Equal(
            new byte[] { 0x00 },
            result);
    }

    [Fact]
    public void Calculate_UnsupportedAlgorithm_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.Calculate(
                new byte[] { 0x01 },
                (ChecksumAlgorithm)999));
    }
}