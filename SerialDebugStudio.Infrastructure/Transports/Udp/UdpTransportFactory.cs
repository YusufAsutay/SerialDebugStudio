using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Infrastructure.Transports.Udp;

public sealed class UdpTransportFactory
    : IUdpTransportFactory
{
    public ITransport Create(
        UdpTransportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new UdpTransport(settings);
    }
}