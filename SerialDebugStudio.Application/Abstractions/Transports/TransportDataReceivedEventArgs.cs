using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Application.Abstractions.Transports;

public sealed class TransportDataReceivedEventArgs : EventArgs
{
    public TransportDataReceivedEventArgs(
        TransportKind transportKind,
        ReadOnlySpan<byte> data,
        DateTimeOffset? timestampUtc = null)
    {
        Record = new DataTransferRecord(
            DataDirection.Received,
            transportKind,
            data,
            timestampUtc);
    }

    public DataTransferRecord Record { get; }

    public TransportKind TransportKind =>
        Record.TransportKind;

    public DataDirection Direction =>
        Record.Direction;

    public DateTimeOffset TimestampUtc =>
        Record.TimestampUtc;

    public ReadOnlyMemory<byte> Data =>
        Record.Data;

    public int ByteCount =>
        Record.ByteCount;
}