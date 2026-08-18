using Xunit;
using SerialDebugStudio.Application.Services.Formatting;

namespace SerialDebugStudio.UnitTests.Formatting;

public sealed class DataConversionServiceBinaryTests
{
    private readonly DataConversionService _service =
        new();

    [Fact]
    public void ParseBinary_WithSeparatedBytes_ReturnsBytes()
    {
        byte[] result =
            _service.ParseBinary(
                "01000001 01000010");

        Assert.Equal(
            new byte[]
            {
                0x41,
                0x42
            },
            result);
    }

    [Fact]
    public void ParseBinary_WithContiguousBits_ReturnsBytes()
    {
        byte[] result =
            _service.ParseBinary(
                "0000000100000011");

        Assert.Equal(
            new byte[]
            {
                0x01,
                0x03
            },
            result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0100000")]
    [InlineData("0000000G")]
    [InlineData("00000002")]
    public void ParseBinary_WithInvalidInput_ThrowsFormatException(
        string input)
    {
        Assert.Throws<FormatException>(
            () => _service.ParseBinary(input));
    }

    [Fact]
    public void FormatBinary_WithBytes_ReturnsSeparatedBits()
    {
        string result =
            _service.FormatBinary(
                new byte[]
                {
                    0x41,
                    0x42
                });

        Assert.Equal(
            "01000001 01000010",
            result);
    }

    [Fact]
    public void FormatBinary_WithEmptyData_ReturnsEmptyString()
    {
        string result =
            _service.FormatBinary(
                Array.Empty<byte>());

        Assert.Equal(
            string.Empty,
            result);
    }
}