using SerialDebugStudio.Application.Services.Formatting;
using SerialDebugStudio.Domain.Enums;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Application;

public sealed class DataConversionServiceTests
{
    private readonly DataConversionService _service =
        new();

    [Fact]
    public void EncodeText_WithAscii_ShouldCreateBytes()
    {
        byte[] result =
            _service.EncodeText(
                "ABC",
                TextEncodingKind.Ascii);

        Assert.Equal(
            new byte[] { 0x41, 0x42, 0x43 },
            result);
    }

    [Fact]
    public void EncodeAndDecode_WithUtf8_ShouldRoundTrip()
    {
        const string source = "Merhaba dünya";

        byte[] encoded =
            _service.EncodeText(
                source,
                TextEncodingKind.Utf8);

        string decoded =
            _service.DecodeText(
                encoded,
                TextEncodingKind.Utf8);

        Assert.Equal(source, decoded);
    }

    [Fact]
    public void ParseHex_WithDifferentSeparators_ShouldCreateBytes()
    {
        byte[] result =
            _service.ParseHex(
                "0x01-03,00 00");

        Assert.Equal(
            new byte[] { 0x01, 0x03, 0x00, 0x00 },
            result);
    }

    [Fact]
    public void ParseHex_WithInvalidCharacter_ShouldThrow()
    {
        Assert.Throws<FormatException>(
            () => _service.ParseHex("01 0G"));
    }

    [Fact]
    public void FormatHex_ShouldCreateUppercaseText()
    {
        byte[] source =
        [
            0x01,
            0x0A,
            0xFF
        ];

        string result =
            _service.FormatHex(source);

        Assert.Equal(
            "01 0A FF",
            result);
    }
}