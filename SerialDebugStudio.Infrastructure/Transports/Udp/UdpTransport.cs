using System.Net;
using System.Net.Sockets;
using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Infrastructure.Transports.Udp;

public sealed class UdpTransport : ITransport
{
    private readonly SemaphoreSlim _operationGate =
        new(1, 1);

    private readonly IPEndPoint _localEndPoint;

    private readonly IPEndPoint _remoteEndPoint;

    private UdpClient? _udpClient;

    private CancellationTokenSource? _receiveCancellationTokenSource;

    private Task? _receiveTask;

    private ConnectionInfo _connectionInfo;

    private bool _disposed;

    public UdpTransport(
        UdpTransportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings = settings;

        _localEndPoint = CreateEndPoint(
            settings.LocalIpAddress,
            settings.LocalPort,
            allowZeroPort: true);

        _remoteEndPoint = CreateEndPoint(
            settings.RemoteIpAddress,
            settings.RemotePort,
            allowZeroPort: false);

        if (_localEndPoint.AddressFamily !=
            _remoteEndPoint.AddressFamily)
        {
            throw new ArgumentException(
                "Yerel ve uzak IP adresleri aynı adres ailesinde olmalıdır.",
                nameof(settings));
        }

        _connectionInfo = new ConnectionInfo(
            TransportKind.Udp,
            ConnectionState.Disconnected,
            CreateConnectionName());
    }

    public TransportKind Kind =>
        TransportKind.Udp;

    public UdpTransportSettings Settings { get; }

    public ConnectionInfo ConnectionInfo =>
        Volatile.Read(ref _connectionInfo);

    public bool IsOpen =>
        Volatile.Read(ref _udpClient) is not null;

    public event EventHandler<TransportDataReceivedEventArgs>?
        DataReceived;

    public event
        EventHandler<TransportConnectionStateChangedEventArgs>?
        ConnectionStateChanged;

    public async Task OpenAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _operationGate
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            ThrowIfDisposed();

            if (_udpClient is not null)
            {
                return;
            }

            ChangeState(ConnectionState.Connecting);

            UdpClient? udpClient = null;

            CancellationTokenSource?
                receiveCancellationTokenSource = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                udpClient = new UdpClient(
                    _localEndPoint);

                receiveCancellationTokenSource =
                    new CancellationTokenSource();

                Volatile.Write(
                    ref _udpClient,
                    udpClient);

                _receiveCancellationTokenSource =
                    receiveCancellationTokenSource;

                _receiveTask = ReceiveLoopAsync(
                    udpClient,
                    receiveCancellationTokenSource.Token);

                ChangeState(ConnectionState.Connected);
            }
            catch (OperationCanceledException)
            {
                udpClient?.Dispose();

                receiveCancellationTokenSource?.Dispose();

                Volatile.Write(
                    ref _udpClient,
                    null);

                _receiveCancellationTokenSource = null;
                _receiveTask = null;

                ChangeState(
                    ConnectionState.Disconnected);

                throw;
            }
            catch (Exception exception)
            {
                udpClient?.Dispose();

                receiveCancellationTokenSource?.Dispose();

                Volatile.Write(
                    ref _udpClient,
                    null);

                _receiveCancellationTokenSource = null;
                _receiveTask = null;

                ChangeState(
                    ConnectionState.Faulted,
                    exception.Message);

                throw;
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task CloseAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _operationGate
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            ThrowIfDisposed();

            await CloseCoreAsync()
                .ConfigureAwait(false);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task SendAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (data.IsEmpty)
        {
            return;
        }

        await _operationGate
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            ThrowIfDisposed();

            var udpClient =
                Volatile.Read(ref _udpClient);

            if (udpClient is null ||
                ConnectionInfo.State !=
                ConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    "UDP dinleyicisi açık değil.");
            }

            try
            {
                await udpClient.SendAsync(
                        data,
                        _remoteEndPoint,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                ChangeState(
                    ConnectionState.Faulted,
                    exception.Message);

                throw;
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task ReceiveLoopAsync(
     UdpClient udpClient,
     CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result =
                    await udpClient
                        .ReceiveAsync(cancellationToken)
                        .ConfigureAwait(false);

                if (result.Buffer.Length == 0)
                {
                    continue;
                }

                var receivedEventArgs =
                    new TransportDataReceivedEventArgs(
                        TransportKind.Udp,
                        result.Buffer);

                DataReceived?.Invoke(
                    this,
                    receivedEventArgs);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException exception)
                when (exception.SocketErrorCode is
                    SocketError.ConnectionReset or
                    SocketError.ConnectionRefused)
            {
                // UDP hedef portunda dinleyici bulunmadığında
                // Windows ICMP Port Unreachable cevabını bu
                // hatalardan biriyle bildirebilir.
                //
                // UDP bağlantısız olduğu için dinleyici
                // kapatılmaz ve yeni datagramlar beklenir.
                continue;
            }
            catch (Exception exception)
            {
                HandleReceiveException(
                    udpClient,
                    exception);

                break;
            }
        }
    }

    private void HandleReceiveException(
        UdpClient source,
        Exception exception)
    {
        if (_disposed)
        {
            return;
        }

        var currentUdpClient =
            Volatile.Read(ref _udpClient);

        if (!ReferenceEquals(
                currentUdpClient,
                source))
        {
            return;
        }

        ChangeState(
            ConnectionState.Faulted,
            exception.Message);
    }

    private async Task CloseCoreAsync()
    {
        var udpClient =
            Volatile.Read(ref _udpClient);

        if (udpClient is null)
        {
            if (ConnectionInfo.State !=
                ConnectionState.Disconnected)
            {
                ChangeState(
                    ConnectionState.Disconnected);
            }

            return;
        }

        ChangeState(ConnectionState.Disconnecting);

        var receiveCancellationTokenSource =
            _receiveCancellationTokenSource;

        var receiveTask =
            _receiveTask;

        Volatile.Write(
            ref _udpClient,
            null);

        _receiveCancellationTokenSource = null;
        _receiveTask = null;

        try
        {
            receiveCancellationTokenSource?.Cancel();

            udpClient.Dispose();

            if (receiveTask is not null)
            {
                try
                {
                    await receiveTask
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Normal kapatma işlemi.
                }
                catch (ObjectDisposedException)
                {
                    // Socket zaten kapatıldı.
                }
                catch (SocketException)
                {
                    // Socket zaten kapatıldı.
                }
            }
        }
        finally
        {
            receiveCancellationTokenSource?.Dispose();

            ChangeState(
                ConnectionState.Disconnected);
        }
    }

    private void ChangeState(
        ConnectionState newState,
        string? errorMessage = null)
    {
        var previousConnection =
            ConnectionInfo;

        var now =
            DateTimeOffset.UtcNow;

        var connectedAtUtc =
            previousConnection.ConnectedAtUtc;

        var disconnectedAtUtc =
            previousConnection.DisconnectedAtUtc;

        if (newState == ConnectionState.Connected)
        {
            connectedAtUtc = now;
            disconnectedAtUtc = null;
        }
        else if (newState ==
                 ConnectionState.Disconnected)
        {
            disconnectedAtUtc = now;
        }

        var currentConnection =
            new ConnectionInfo(
                TransportKind.Udp,
                newState,
                CreateConnectionName(),
                connectedAtUtc,
                disconnectedAtUtc,
                errorMessage);

        Volatile.Write(
            ref _connectionInfo,
            currentConnection);

        var stateChangedEventArgs =
            new TransportConnectionStateChangedEventArgs(
                previousConnection.State,
                currentConnection,
                now);

        ConnectionStateChanged?.Invoke(
            this,
            stateChangedEventArgs);
    }

    private string CreateConnectionName()
    {
        return
            $"{_localEndPoint} → {_remoteEndPoint}";
    }

    private static IPEndPoint CreateEndPoint(
        string ipAddressText,
        int port,
        bool allowZeroPort)
    {
        if (string.IsNullOrWhiteSpace(
                ipAddressText))
        {
            throw new ArgumentException(
                "IP adresi boş bırakılamaz.",
                nameof(ipAddressText));
        }

        if (!IPAddress.TryParse(
                ipAddressText,
                out var ipAddress))
        {
            throw new ArgumentException(
                $"Geçersiz IP adresi: {ipAddressText}",
                nameof(ipAddressText));
        }

        var minimumPort =
            allowZeroPort ? 0 : 1;

        if (port < minimumPort ||
            port > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port),
                $"Port değeri {minimumPort}-65535 arasında olmalıdır.");
        }

        return new IPEndPoint(
            ipAddress,
            port);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(UdpTransport));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _operationGate
            .WaitAsync()
            .ConfigureAwait(false);

        try
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                await CloseCoreAsync()
                    .ConfigureAwait(false);
            }
            finally
            {
                _disposed = true;
            }
        }
        finally
        {
            _operationGate.Release();
        }

        GC.SuppressFinalize(this);
    }
}