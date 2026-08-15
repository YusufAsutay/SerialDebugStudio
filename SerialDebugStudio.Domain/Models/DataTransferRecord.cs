using SerialDebugStudio.Domain.Enums;

namespace SerialDebugStudio.Domain.Models;

public sealed class DataTransferRecord
{
    private readonly byte[] _data;

    public DataTransferRecord(
        DataDirection direction,
        TransportKind transportKind,
        ReadOnlySpan<byte> data,
        DateTimeOffset? timestampUtc = null)
    {
        Id = Guid.NewGuid();
        Direction = direction;
        TransportKind = transportKind;
        TimestampUtc = timestampUtc ?? DateTimeOffset.UtcNow;

        _data = data.ToArray();
    }

    public Guid Id { get; }

    public DataDirection Direction { get; }

    public TransportKind TransportKind { get; }

    public DateTimeOffset TimestampUtc { get; }

    public ReadOnlyMemory<byte> Data => _data;

    public int ByteCount => _data.Length;
}