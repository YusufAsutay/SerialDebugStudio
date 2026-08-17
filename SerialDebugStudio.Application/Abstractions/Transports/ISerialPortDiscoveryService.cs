namespace SerialDebugStudio.Application.Abstractions.Transports;

public interface ISerialPortDiscoveryService
{
    IReadOnlyList<string> GetAvailablePortNames();
}