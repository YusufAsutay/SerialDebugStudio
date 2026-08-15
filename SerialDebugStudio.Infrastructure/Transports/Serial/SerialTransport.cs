using System.IO.Ports;
using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Infrastructure.Transports.Serial;

public sealed class SerialTransport : ITransport
{
    private readonly SerialPort _serialPort;

    private readonly SemaphoreSlim _operationGate =
        new(1, 1);

    private ConnectionInfo _connectionInfo;

    private bool _disposed;

    public SerialTransport(
        SerialPortSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings = settings;
        _serialPort = SerialPortFactory.Create(settings);

        _serialPort.DataReceived +=
            OnSerialPortDataReceived;

        _connectionInfo = new ConnectionInfo(
            TransportKind.Serial,
            ConnectionState.Disconnected,
            settings.PortName);
    }

    public TransportKind Kind =>
        TransportKind.Serial;

    public SerialPortSettings Settings { get; }

    public ConnectionInfo ConnectionInfo =>
        Volatile.Read(ref _connectionInfo);

    public bool IsOpen =>
        _serialPort.IsOpen;

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

            if (_serialPort.IsOpen)
            {
                return;
            }

            ChangeState(ConnectionState.Connecting);

            try
            {
                await Task.Run(
                    _serialPort.Open,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                ChangeState(ConnectionState.Disconnected);
                throw;
            }
            catch (Exception exception)
            {
                ChangeState(
                    ConnectionState.Faulted,
                    exception.Message);

                throw;
            }

            ChangeState(ConnectionState.Connected);
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

            if (!_serialPort.IsOpen)
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

            try
            {
                await Task.Run(_serialPort.Close)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                ChangeState(
                    ConnectionState.Faulted,
                    exception.Message);

                throw;
            }

            ChangeState(ConnectionState.Disconnected);
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

            if (!_serialPort.IsOpen ||
                ConnectionInfo.State !=
                ConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    "Seri port açık değil.");
            }

            try
            {
                await _serialPort.BaseStream
                    .WriteAsync(data, cancellationToken)
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

    private void OnSerialPortDataReceived(
        object sender,
        SerialDataReceivedEventArgs eventArgs)
    {
        if (_disposed || !_serialPort.IsOpen)
        {
            return;
        }

        try
        {
            var availableByteCount =
                _serialPort.BytesToRead;

            if (availableByteCount <= 0)
            {
                return;
            }

            var buffer =
                new byte[availableByteCount];

            var readByteCount = _serialPort.Read(
                buffer,
                0,
                buffer.Length);

            if (readByteCount <= 0)
            {
                return;
            }

            if (readByteCount != buffer.Length)
            {
                Array.Resize(
                    ref buffer,
                    readByteCount);
            }

            var receivedEventArgs =
                new TransportDataReceivedEventArgs(
                    TransportKind.Serial,
                    buffer);

            DataReceived?.Invoke(
                this,
                receivedEventArgs);
        }
        catch (InvalidOperationException exception)
        {
            HandleReceiveException(exception);
        }
        catch (IOException exception)
        {
            HandleReceiveException(exception);
        }
    }

    private void HandleReceiveException(
        Exception exception)
    {
        if (_disposed || !_serialPort.IsOpen)
        {
            return;
        }

        ChangeState(
            ConnectionState.Faulted,
            exception.Message);
    }

    private void ChangeState(
        ConnectionState newState,
        string? errorMessage = null)
    {
        var previousConnection =
            ConnectionInfo;

        var now = DateTimeOffset.UtcNow;

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

        var currentConnection = new ConnectionInfo(
            TransportKind.Serial,
            newState,
            Settings.PortName,
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

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(SerialTransport));
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
                if (_serialPort.IsOpen)
                {
                    ChangeState(
                        ConnectionState.Disconnecting);

                    await Task.Run(_serialPort.Close)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                _serialPort.DataReceived -=
                    OnSerialPortDataReceived;

                _serialPort.Dispose();

                if (ConnectionInfo.State !=
                    ConnectionState.Disconnected)
                {
                    ChangeState(
                        ConnectionState.Disconnected);
                }

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