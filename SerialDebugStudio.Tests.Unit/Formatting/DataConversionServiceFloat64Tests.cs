using SerialDebugStudio.Application.Services.Formatting;
using SerialDebugStudio.Domain.Enums;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Formatting;

public sealed class DataConversionServiceFloat64Tests
{
    private readonly DataConversionService _service = new();

    [Fact]
    public void ParseIeee754_Float64BigEndian_ReturnsExpectedBytes()
    {
        byte[] result = _service.ParseIeee754(
            "1.5",
            NumericDataType.Float64,
            ByteOrder.BigEndian);

        byte[] expected =
        {
            0x3F,
            0xF8,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseIeee754_Float64LittleEndian_ReturnsExpectedBytes()
    {
        byte[] result = _service.ParseIeee754(
            "1.5",
            NumericDataType.Float64,
            ByteOrder.LittleEndian);

        byte[] expected =
        {
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0xF8,
            0x3F
        };

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("12.5")]
    [InlineData("12,5")]
    public void ParseIeee754_Float64BigEndian_AcceptsDotAndComma(
        string input)
    {
        byte[] result = _service.ParseIeee754(
            input,
            NumericDataType.Float64,
            ByteOrder.BigEndian);

        byte[] expected =
        {
            0x40,
            0x29,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseIeee754_Float64LittleEndian_ReturnsExpectedBytesForTwelvePointFive()
    {
        byte[] result = _service.ParseIeee754(
            "12.5",
            NumericDataType.Float64,
            ByteOrder.LittleEndian);

        byte[] expected =
        {
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x29,
            0x40
        };

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABC")]
    [InlineData("12.5.6")]
    public void ParseIeee754_InvalidFloat64_ThrowsFormatException(
        string input)
    {
        Assert.Throws<FormatException>(() =>
            _service.ParseIeee754(
                input,
                NumericDataType.Float64,
                ByteOrder.BigEndian));
    }

    [Fact]
    public void ParseIeee754_Float64Overflow_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() =>
            _service.ParseIeee754(
                "1e5000",
                NumericDataType.Float64,
                ByteOrder.BigEndian));
    }
}