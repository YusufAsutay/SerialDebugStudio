using System.IO.Ports;
using SerialDebugStudio.Application.Abstractions.Transports;

namespace SerialDebugStudio.Infrastructure.Transports.Serial;

public sealed class SerialPortDiscoveryService
    : ISerialPortDiscoveryService
{
    public IReadOnlyList<string> GetAvailablePortNames()
    {
        return SerialPort
            .GetPortNames()
            .OrderBy(portName => portName)
            .ToArray();
    }
}