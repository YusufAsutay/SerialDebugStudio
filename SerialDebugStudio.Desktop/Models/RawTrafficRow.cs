using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Desktop.Models;

public sealed class RawTrafficRow
{
    private readonly byte[] _data;

    public RawTrafficRow(
        DataTransferRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        Direction = record.Direction;
        TransportKind = record.TransportKind;
        TimestampUtc = record.TimestampUtc;
        ByteCount = record.ByteCount;

        _data = record.Data.ToArray();
    }

    public DataDirection Direction { get; }

    public TransportKind TransportKind { get; }

    public DateTimeOffset TimestampUtc { get; }

    public int ByteCount { get; }

    public string DirectionText =>
        Direction == DataDirection.Received
            ? "RX"
            : "TX";

    public string TimestampText =>
        TimestampUtc
            .ToLocalTime()
            .ToString("HH:mm:ss.fff");

    public string RawByteText =>
        string.Join(
            " ",
            _data.Select(
                value => value.ToString("X2")));
}