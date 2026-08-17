using System.Globalization;
using System.Text;
using SerialDebugStudio.Application.Abstractions.Formatting;
using SerialDebugStudio.Domain.Enums;

namespace SerialDebugStudio.Application.Services.Formatting;

public sealed class DataConversionService
    : IDataConversionService
{
    private static readonly Encoding StrictAscii =
        CreateStrictEncoding(Encoding.ASCII);

    private static readonly Encoding StrictUtf8 =
        CreateStrictEncoding(
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true));

    private static readonly Encoding StrictUtf16LittleEndian =
        CreateStrictEncoding(
            new UnicodeEncoding(
                bigEndian: false,
                byteOrderMark: false,
                throwOnInvalidBytes: true));

    private static readonly Encoding StrictUtf16BigEndian =
        CreateStrictEncoding(
            new UnicodeEncoding(
                bigEndian: true,
                byteOrderMark: false,
                throwOnInvalidBytes: true));

    private static readonly Encoding StrictUtf32LittleEndian =
        CreateStrictEncoding(
            new UTF32Encoding(
                bigEndian: false,
                byteOrderMark: false,
                throwOnInvalidCharacters: true));

    public byte[] EncodeText(
        string text,
        TextEncodingKind encodingKind)
    {
        ArgumentNullException.ThrowIfNull(text);

        return GetEncoding(encodingKind)
            .GetBytes(text);
    }

    public string DecodeText(
        ReadOnlySpan<byte> data,
        TextEncodingKind encodingKind)
    {
        return GetEncoding(encodingKind)
            .GetString(data);
    }

    public byte[] ParseHex(string hexText)
    {
        ArgumentNullException.ThrowIfNull(hexText);

        string withoutPrefixes =
            hexText.Replace(
                "0x",
                string.Empty,
                StringComparison.OrdinalIgnoreCase);

        var normalized = new StringBuilder();

        foreach (char character in withoutPrefixes)
        {
            if (char.IsWhiteSpace(character) ||
                character is '-' or ',' or ';' or ':')
            {
                continue;
            }

            normalized.Append(character);
        }

        if (normalized.Length == 0)
        {
            return [];
        }

        if (normalized.Length % 2 != 0)
        {
            throw new FormatException(
                "HEX veri çift sayıda karakter içermelidir.");
        }

        var result =
            new byte[normalized.Length / 2];

        for (int index = 0;
             index < normalized.Length;
             index += 2)
        {
            string byteText =
                normalized.ToString(index, 2);

            if (!byte.TryParse(
                    byteText,
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out byte value))
            {
                throw new FormatException(
                    $"Geçersiz HEX byte değeri: {byteText}");
            }

            result[index / 2] = value;
        }

        return result;
    }

    public string FormatHex(
        ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        return string.Join(
            " ",
            data
                .ToArray()
                .Select(
                    value =>
                        value.ToString(
                            "X2",
                            CultureInfo.InvariantCulture)));
    }

    private static Encoding GetEncoding(
        TextEncodingKind encodingKind)
    {
        return encodingKind switch
        {
            TextEncodingKind.Ascii =>
                StrictAscii,

            TextEncodingKind.Utf8 =>
                StrictUtf8,

            TextEncodingKind.Utf16LittleEndian =>
                StrictUtf16LittleEndian,

            TextEncodingKind.Utf16BigEndian =>
                StrictUtf16BigEndian,

            TextEncodingKind.Utf32LittleEndian =>
                StrictUtf32LittleEndian,

            _ => throw new ArgumentOutOfRangeException(
                nameof(encodingKind),
                encodingKind,
                "Desteklenmeyen encoding türü.")
        };
    }

    private static Encoding CreateStrictEncoding(
        Encoding source)
    {
        var result =
            (Encoding)source.Clone();

        result.EncoderFallback =
            EncoderFallback.ExceptionFallback;

        result.DecoderFallback =
            DecoderFallback.ExceptionFallback;

        return result;
        public byte[] ParseBinary(string binaryText)
{
    if (string.IsNullOrWhiteSpace(binaryText))
    {
        throw new FormatException(
            "Binary veri boş olamaz.");
    }

    var normalizedBuilder =
        new StringBuilder(binaryText.Length);

    foreach (char character in binaryText)
    {
        if (char.IsWhiteSpace(character))
        {
            continue;
        }

        if (character is not ('0' or '1'))
        {
            throw new FormatException(
                $"Binary veri yalnızca 0 ve 1 içerebilir. " +
                $"Geçersiz karakter: '{character}'");
        }

        normalizedBuilder.Append(character);
    }

    string normalized =
        normalizedBuilder.ToString();

    if (normalized.Length % 8 != 0)
    {
        throw new FormatException(
            "Binary veride her byte tam olarak 8 bit olmalıdır.");
    }

    var result =
        new byte[normalized.Length / 8];

    for (int byteIndex = 0;
         byteIndex < result.Length;
         byteIndex++)
    {
        string byteText =
            normalized.Substring(
                byteIndex * 8,
                8);

        result[byteIndex] =
            Convert.ToByte(byteText, 2);
    }

    return result;
}

public string FormatBinary(
    ReadOnlySpan<byte> data)
{
    if (data.IsEmpty)
    {
        return string.Empty;
    }

    var result =
        new StringBuilder(data.Length * 9);

    for (int index = 0;
         index < data.Length;
         index++)
    {
        if (index > 0)
        {
            result.Append(' ');
        }

        result.Append(
            Convert
                .ToString(data[index], 2)
                .PadLeft(8, '0'));
    }

    return result.ToString();
}


    }
}