using SerialDebugStudio.Application.Abstractions.Checksums;
using SerialDebugStudio.Domain.Enums;

namespace SerialDebugStudio.Application.Services.Checksums;

public sealed class ChecksumService
    : IChecksumService
{
    public byte[] Calculate(
        ReadOnlySpan<byte> data,
        ChecksumAlgorithm algorithm)
    {
        return algorithm switch
        {
            ChecksumAlgorithm.None =>
                Array.Empty<byte>(),

            ChecksumAlgorithm.Sum8 =>
                new[] { CalculateSum8(data) },

            ChecksumAlgorithm.Crc8OpenSafety =>
                new[]
                {
            CalculateCrc8OpenSafety(data)
                },

            ChecksumAlgorithm.Crc8Smbus =>
                new[]
                {
            CalculateCrc8Smbus(data)
                },

            ChecksumAlgorithm.Crc16Modbus =>
                CalculateCrc16Modbus(data),

            ChecksumAlgorithm.Crc16Gsm =>
                CalculateCrc16Gsm(data),

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(algorithm),
                    algorithm,
                    "Desteklenmeyen checksum algoritması.")
        };
    }

    public byte[] Append(
        ReadOnlySpan<byte> data,
        ChecksumAlgorithm algorithm)
    {
        byte[] checksum = Calculate(
            data,
            algorithm);

        byte[] result =
            new byte[data.Length + checksum.Length];

        data.CopyTo(result.AsSpan());

        checksum
            .AsSpan()
            .CopyTo(result.AsSpan(data.Length));

        return result;
    }

    private static byte CalculateSum8(
        ReadOnlySpan<byte> data)
    {
        int sum = 0;

        foreach (byte value in data)
        {
            sum += value;
        }

        return (byte)(sum & 0xFF);
    }


    private static byte CalculateCrc8OpenSafety(
     ReadOnlySpan<byte> data)
    {
        return CalculateCrc8(
            data,
            polynomial: 0x2F);
    }

    private static byte CalculateCrc8Smbus(
        ReadOnlySpan<byte> data)
    {
        return CalculateCrc8(
            data,
            polynomial: 0x07);
    }

    private static byte CalculateCrc8(
        ReadOnlySpan<byte> data,
        byte polynomial)
    {
        byte crc = 0x00;

        foreach (byte value in data)
        {
            crc ^= value;

            for (int bit = 0; bit < 8; bit++)
            {
                bool mostSignificantBitIsSet =
                    (crc & 0x80) != 0;

                crc = mostSignificantBitIsSet
                    ? (byte)((crc << 1) ^ polynomial)
                    : (byte)(crc << 1);
            }
        }

        return crc;
    }


    private static byte[] CalculateCrc16Modbus(
    ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;

        foreach (byte value in data)
        {
            crc ^= value;

            for (int bit = 0; bit < 8; bit++)
            {
                bool leastSignificantBitIsSet =
                    (crc & 0x0001) != 0;

                crc >>= 1;

                if (leastSignificantBitIsSet)
                {
                    crc ^= 0xA001;
                }
            }
        }

        byte lowByte =
            (byte)(crc & 0x00FF);

        byte highByte =
            (byte)((crc >> 8) & 0x00FF);

        return new[]
        {
        lowByte,
        highByte
    };

    }
    private static byte[] CalculateCrc16Gsm(
    ReadOnlySpan<byte> data)
    {
        const ushort polynomial = 0x1021;
        const ushort finalXor = 0xFFFF;

        ushort crc = 0x0000;

        foreach (byte value in data)
        {
            crc ^= (ushort)(value << 8);

            for (int bit = 0; bit < 8; bit++)
            {
                bool mostSignificantBitIsSet =
                    (crc & 0x8000) != 0;

                crc = mostSignificantBitIsSet
                    ? (ushort)((crc << 1) ^ polynomial)
                    : (ushort)(crc << 1);
            }
        }

        crc ^= finalXor;

        byte highByte =
            (byte)((crc >> 8) & 0x00FF);

        byte lowByte =
            (byte)(crc & 0x00FF);

        return new[]
        {
        highByte,
        lowByte
    };
    }

}