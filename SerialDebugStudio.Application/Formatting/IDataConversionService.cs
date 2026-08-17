using SerialDebugStudio.Domain.Enums;

namespace SerialDebugStudio.Application.Abstractions.Formatting;

public interface IDataConversionService
{
    byte[] EncodeText(
        string text,
        TextEncodingKind encodingKind);

    string DecodeText(
        ReadOnlySpan<byte> data,
        TextEncodingKind encodingKind);

    byte[] ParseHex(string hexText);

    string FormatHex(ReadOnlySpan<byte> data);


}