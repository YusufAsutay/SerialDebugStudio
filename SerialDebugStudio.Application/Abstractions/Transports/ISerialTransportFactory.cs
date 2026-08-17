using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Application.Abstractions.Transports;

public interface ISerialTransportFactory
{
    ITransport Create(SerialPortSettings settings);
}