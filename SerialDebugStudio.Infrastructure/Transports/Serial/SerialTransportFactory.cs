using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Infrastructure.Transports.Serial;

public sealed class SerialTransportFactory
    : ISerialTransportFactory
{
    public ITransport Create(SerialPortSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new SerialTransport(settings);
    }
}