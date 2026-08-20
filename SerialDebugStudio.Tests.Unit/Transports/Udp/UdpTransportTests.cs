using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;
using SerialDebugStudio.Infrastructure.Transports.Udp;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Transports.Udp;

public sealed class UdpTransportTests
{
    [Fact]
    public void Constructor_CreatesDisconnectedUdpTransport()
    {
        var settings = CreateSettings();

        var transport =
            new UdpTransport(settings);

        Assert.Equal(
            TransportKind.Udp,
            transport.Kind);

        Assert.Equal(
            ConnectionState.Disconnected,
            transport.ConnectionInfo.State);

        Assert.False(transport.IsOpen);
        Assert.Same(settings, transport.Settings);
    }

    [Fact]
    public async Task OpenAsync_OpensUdpListener()
    {
        await using var transport =
            new UdpTransport(CreateSettings());

        await transport.OpenAsync();

        Assert.True(transport.IsOpen);

        Assert.Equal(
            ConnectionState.Connected,
            transport.ConnectionInfo.State);

        Assert.NotNull(
            transport.ConnectionInfo.ConnectedAtUtc);
    }

    [Fact]
    public async Task SendAsync_SendsDatagramToRemoteEndpoint()
    {
        using var receiver =
            new UdpClient(
                new IPEndPoint(
                    IPAddress.Loopback,
                    0));

        var receiverEndPoint =
            (IPEndPoint)receiver.Client.LocalEndPoint!;

        var settings = new UdpTransportSettings
        {
            LocalIpAddress = "127.0.0.1",
            LocalPort = 0,
            RemoteIpAddress = "127.0.0.1",
            RemotePort = receiverEndPoint.Port
        };

        await using var transport =
            new UdpTransport(settings);

        await transport.OpenAsync();

        byte[] expectedData =
        {
            0x01,
            0x03,
            0x00,
            0x00,
            0x00,
            0x0A
        };

        await transport.SendAsync(expectedData);

        using var timeoutCancellationTokenSource =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(5));

        var receivedResult =
            await receiver.ReceiveAsync(
                timeoutCancellationTokenSource.Token);

        Assert.Equal(
            expectedData,
            receivedResult.Buffer);
    }

    [Fact]
    public async Task ReceiveAsync_RaisesDataReceivedEvent()
    {
        var localPort =
            GetAvailableUdpPort();

        var settings = new UdpTransportSettings
        {
            LocalIpAddress = "127.0.0.1",
            LocalPort = localPort,
            RemoteIpAddress = "127.0.0.1",
            RemotePort = 5001
        };

        await using var transport =
            new UdpTransport(settings);

        var receivedDataSource =
            new TaskCompletionSource<
                TransportDataReceivedEventArgs>(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

        transport.DataReceived +=
            (_, eventArgs) =>
            {
                receivedDataSource.TrySetResult(
                    eventArgs);
            };

        await transport.OpenAsync();

        using var sender =
            new UdpClient(AddressFamily.InterNetwork);

        byte[] expectedData =
        {
            0x48,
            0x45,
            0x4C,
            0x4C,
            0x4F
        };

        await sender.SendAsync(
            expectedData,
            expectedData.Length,
            new IPEndPoint(
                IPAddress.Loopback,
                localPort));

        var receivedEventArgs =
            await receivedDataSource.Task.WaitAsync(
                TimeSpan.FromSeconds(5));

        Assert.Equal(
            TransportKind.Udp,
            receivedEventArgs.TransportKind);

        Assert.Equal(
            DataDirection.Received,
            receivedEventArgs.Direction);

        Assert.Equal(
            expectedData.Length,
            receivedEventArgs.ByteCount);

        Assert.Equal(
            expectedData,
            receivedEventArgs.Data.ToArray());
    }

    [Fact]
    public async Task OpenAndCloseAsync_RaisesExpectedStateChanges()
    {
        await using var transport =
            new UdpTransport(CreateSettings());

        var observedStates =
            new List<ConnectionState>();

        transport.ConnectionStateChanged +=
            (_, _) =>
            {
                observedStates.Add(
                    transport.ConnectionInfo.State);
            };

        await transport.OpenAsync();
        await transport.CloseAsync();

        ConnectionState[] expectedStates =
        {
            ConnectionState.Connecting,
            ConnectionState.Connected,
            ConnectionState.Disconnecting,
            ConnectionState.Disconnected
        };

        Assert.Equal(
            expectedStates,
            observedStates);

        Assert.False(transport.IsOpen);

        Assert.Equal(
            ConnectionState.Disconnected,
            transport.ConnectionInfo.State);

        Assert.NotNull(
            transport.ConnectionInfo.DisconnectedAtUtc);
    }

    [Fact]
    public async Task CloseAsync_WhenAlreadyClosed_RemainsDisconnected()
    {
        await using var transport =
            new UdpTransport(CreateSettings());

        await transport.CloseAsync();

        Assert.False(transport.IsOpen);

        Assert.Equal(
            ConnectionState.Disconnected,
            transport.ConnectionInfo.State);
    }

    [Fact]
    public async Task SendAsync_WhenTransportIsClosed_ThrowsException()
    {
        await using var transport =
            new UdpTransport(CreateSettings());

        byte[] data =
        {
            0x01,
            0x02,
            0x03
        };

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () => transport.SendAsync(data));

        Assert.Equal(
            "UDP dinleyicisi açık değil.",
            exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_ClosesTransport()
    {
        var transport =
            new UdpTransport(CreateSettings());

        await transport.OpenAsync();

        Assert.True(transport.IsOpen);

        await transport.DisposeAsync();

        Assert.False(transport.IsOpen);

        Assert.Equal(
            ConnectionState.Disconnected,
            transport.ConnectionInfo.State);

        await Assert.ThrowsAsync<
            ObjectDisposedException>(
                () => transport.OpenAsync());
    }

    private static UdpTransportSettings CreateSettings()
    {
        return new UdpTransportSettings
        {
            LocalIpAddress = "127.0.0.1",
            LocalPort = 0,
            RemoteIpAddress = "127.0.0.1",
            RemotePort = 5001
        };
    }

    private static int GetAvailableUdpPort()
    {
        using var udpClient =
            new UdpClient(
                new IPEndPoint(
                    IPAddress.Loopback,
                    0));

        var localEndPoint =
            (IPEndPoint)udpClient
                .Client
                .LocalEndPoint!;

        return localEndPoint.Port;
    }
}