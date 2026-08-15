using SerialDebugStudio.Domain.Enums;

namespace SerialDebugStudio.Domain.Models;

public sealed record SerialPortSettings
{
    public string PortName { get; init; } = string.Empty;

    public int BaudRate { get; init; } = 9600;

    public SerialDataBits DataBits { get; init; } =
        SerialDataBits.Eight;

    public SerialParity Parity { get; init; } =
        SerialParity.None;

    public SerialStopBits StopBits { get; init; } =
        SerialStopBits.One;

    public SerialFlowControl FlowControl { get; init; } =
        SerialFlowControl.None;

    public bool DtrEnable { get; init; }

    public bool RtsEnable { get; init; }

    public int ReadTimeoutMilliseconds { get; init; } = 1000;

    public int WriteTimeoutMilliseconds { get; init; } = 1000;
}