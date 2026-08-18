using Xunit;
using SerialDebugStudio.Application.Services.Formatting;
using SerialDebugStudio.Domain.Enums;

namespace SerialDebugStudio.Tests.Unit.Formatting;

public sealed class DataConversionServiceFloat32Tests
{
    private readonly DataConversionService _service = new();

    [Fact]
    public void ParseIeee754_Float32BigEndian_ReturnsExpectedBytes()
    {
        byte[] result = _service.ParseIeee754(
            "1.5",
            NumericDataType.Float32,
            ByteOrder.BigEndian);

        byte[] expected =
        {
            0x3F,
            0xC0,
            0x00,
            0x00
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseIeee754_Float32LittleEndian_ReturnsExpectedBytes()
    {
        byte[] result = _service.ParseIeee754(
            "1.5",
            NumericDataType.Float32,
            ByteOrder.LittleEndian);

        byte[] expected =
        {
            0x00,
            0x00,
            0xC0,
            0x3F
        };

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("12.5")]
    [InlineData("12,5")]
    public void ParseIeee754_Float32BigEndian_AcceptsDotAndComma(
        string input)
    {
        byte[] result = _service.ParseIeee754(
            input,
            NumericDataType.Float32,
            ByteOrder.BigEndian);

        byte[] expected =
        {
            0x41,
            0x48,
            0x00,
            0x00
        };

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABC")]
    [InlineData("12.5.6")]
    public void ParseIeee754_InvalidFloat32_ThrowsFormatException(
        string input)
    {
        Assert.Throws<FormatException>(() =>
            _service.ParseIeee754(
                input,
                NumericDataType.Float32,
                ByteOrder.BigEndian));
    }

    [Fact]
    public void ParseIeee754_Float32Overflow_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() =>
            _service.ParseIeee754(
                "1e100",
                NumericDataType.Float32,
                ByteOrder.BigEndian));
    }


}