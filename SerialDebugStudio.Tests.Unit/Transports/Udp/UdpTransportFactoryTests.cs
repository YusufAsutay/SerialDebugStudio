using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;
using SerialDebugStudio.Infrastructure.Transports.Udp;
using Xunit;

namespace SerialDebugStudio.Tests.Unit.Transports.Udp;

public sealed class UdpTransportFactoryTests
{
    private readonly UdpTransportFactory _factory =
        new();

    [Fact]
    public async Task Create_ReturnsConfiguredUdpTransport()
    {
        var settings = CreateSettings();

        await using ITransport transport =
            _factory.Create(settings);

        var udpTransport =
            Assert.IsType<UdpTransport>(
                transport);

        Assert.Equal(
            TransportKind.Udp,
            udpTransport.Kind);

        Assert.Same(
            settings,
            udpTransport.Settings);

        Assert.False(
            udpTransport.IsOpen);

        Assert.Equal(
            ConnectionState.Disconnected,
            udpTransport.ConnectionInfo.State);
    }

    [Fact]
    public async Task Create_CalledTwice_ReturnsDifferentInstances()
    {
        var firstSettings =
            new UdpTransportSettings
            {
                LocalIpAddress = "127.0.0.1",
                LocalPort = 5000,
                RemoteIpAddress = "127.0.0.1",
                RemotePort = 5001
            };

        var secondSettings =
            new UdpTransportSettings
            {
                LocalIpAddress = "127.0.0.1",
                LocalPort = 5001,
                RemoteIpAddress = "127.0.0.1",
                RemotePort = 5000
            };

        await using ITransport firstTransport =
            _factory.Create(firstSettings);

        await using ITransport secondTransport =
            _factory.Create(secondSettings);

        Assert.NotSame(
            firstTransport,
            secondTransport);

        Assert.IsType<UdpTransport>(
            firstTransport);

        Assert.IsType<UdpTransport>(
            secondTransport);
    }

    [Fact]
    public void Create_WithNullSettings_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _factory.Create(null!));
    }

    private static UdpTransportSettings CreateSettings()
    {
        return new UdpTransportSettings
        {
            LocalIpAddress = "127.0.0.1",
            LocalPort = 5000,
            RemoteIpAddress = "127.0.0.1",
            RemotePort = 5001
        };
    }
}