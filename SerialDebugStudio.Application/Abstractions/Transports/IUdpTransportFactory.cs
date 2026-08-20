using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Application.Abstractions.Transports;

public interface IUdpTransportFactory
{
    ITransport Create(
        UdpTransportSettings settings);
}