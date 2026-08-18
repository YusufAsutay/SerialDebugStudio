using SerialDebugStudio.Application.Services.Formatting;
using SerialDebugStudio.Domain.Enums;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Formatting;

public sealed class DataConversionServiceLineEndingTests
{
    private readonly DataConversionService _service = new();

    [Fact]
    public void ApplyLineEnding_None_ReturnsOriginalText()
    {
        string result = _service.ApplyLineEnding(
            "HELLO",
            LineEnding.None);

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void ApplyLineEnding_CR_AppendsCarriageReturn()
    {
        string result = _service.ApplyLineEnding(
            "HELLO",
            LineEnding.CR);

        Assert.Equal("HELLO\r", result);
    }

    [Fact]
    public void ApplyLineEnding_LF_AppendsLineFeed()
    {
        string result = _service.ApplyLineEnding(
            "HELLO",
            LineEnding.LF);

        Assert.Equal("HELLO\n", result);
    }

    [Fact]
    public void ApplyLineEnding_CRLF_AppendsCarriageReturnAndLineFeed()
    {
        string result = _service.ApplyLineEnding(
            "HELLO",
            LineEnding.CRLF);

        Assert.Equal("HELLO\r\n", result);
    }

    [Fact]
    public void ApplyLineEnding_CRLF_WithEmptyText_ReturnsOnlyLineEnding()
    {
        string result = _service.ApplyLineEnding(
            string.Empty,
            LineEnding.CRLF);

        Assert.Equal("\r\n", result);
    }

    [Fact]
    public void ApplyLineEnding_NullText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _service.ApplyLineEnding(
                null!,
                LineEnding.None));
    }

    [Fact]
    public void ApplyLineEnding_UnsupportedSelection_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.ApplyLineEnding(
                "HELLO",
                (LineEnding)999));
    }
}