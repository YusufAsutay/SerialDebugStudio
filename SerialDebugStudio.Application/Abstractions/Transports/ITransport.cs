using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Application.Abstractions.Transports;

public interface ITransport : IAsyncDisposable
{
    TransportKind Kind { get; }

    ConnectionInfo ConnectionInfo { get; }

    bool IsOpen { get; }

    event EventHandler<TransportDataReceivedEventArgs>? DataReceived;

    event EventHandler<TransportConnectionStateChangedEventArgs>?
        ConnectionStateChanged;

    Task OpenAsync(
        CancellationToken cancellationToken = default);

    Task CloseAsync(
        CancellationToken cancellationToken = default);

    Task SendAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);
}