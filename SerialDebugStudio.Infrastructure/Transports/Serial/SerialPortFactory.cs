using System.IO.Ports;
using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;

namespace SerialDebugStudio.Infrastructure.Transports.Serial;

internal static class SerialPortFactory
{
    public static SerialPort Create(
        SerialPortSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ValidateSettings(settings);

        var handshake = MapFlowControl(
            settings.FlowControl);

        var serialPort = new SerialPort(
            settings.PortName,
            settings.BaudRate,
            MapParity(settings.Parity),
            MapDataBits(settings.DataBits),
            MapStopBits(settings.StopBits))
        {
            Handshake = handshake,
            DtrEnable = settings.DtrEnable,
            ReadTimeout = settings.ReadTimeoutMilliseconds,
            WriteTimeout = settings.WriteTimeoutMilliseconds
        };

        if (handshake is Handshake.None or Handshake.XOnXOff)
        {
            serialPort.RtsEnable = settings.RtsEnable;
        }

        return serialPort;
    }

    private static void ValidateSettings(
        SerialPortSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PortName))
        {
            throw new ArgumentException(
                "Seri port adı boş olamaz.",
                nameof(settings));
        }

        if (settings.BaudRate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                "Baud rate sıfırdan büyük olmalıdır.");
        }

        ValidateTimeout(
            settings.ReadTimeoutMilliseconds,
            nameof(settings.ReadTimeoutMilliseconds));

        ValidateTimeout(
            settings.WriteTimeoutMilliseconds,
            nameof(settings.WriteTimeoutMilliseconds));
    }

    private static void ValidateTimeout(
        int timeout,
        string parameterName)
    {
        if (timeout <= 0 &&
            timeout != SerialPort.InfiniteTimeout)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Timeout pozitif veya -1 olmalıdır.");
        }
    }

    private static int MapDataBits(
        SerialDataBits dataBits)
    {
        return dataBits switch
        {
            SerialDataBits.Five => 5,
            SerialDataBits.Six => 6,
            SerialDataBits.Seven => 7,
            SerialDataBits.Eight => 8,

            _ => throw new ArgumentOutOfRangeException(
                nameof(dataBits),
                dataBits,
                "Desteklenmeyen data bits değeri.")
        };
    }

    private static Parity MapParity(
        SerialParity parity)
    {
        return parity switch
        {
            SerialParity.None => Parity.None,
            SerialParity.Odd => Parity.Odd,
            SerialParity.Even => Parity.Even,
            SerialParity.Mark => Parity.Mark,
            SerialParity.Space => Parity.Space,

            _ => throw new ArgumentOutOfRangeException(
                nameof(parity),
                parity,
                "Desteklenmeyen parity değeri.")
        };
    }

    private static StopBits MapStopBits(
        SerialStopBits stopBits)
    {
        return stopBits switch
        {
            SerialStopBits.One => StopBits.One,
            SerialStopBits.OnePointFive =>
                StopBits.OnePointFive,
            SerialStopBits.Two => StopBits.Two,

            _ => throw new ArgumentOutOfRangeException(
                nameof(stopBits),
                stopBits,
                "Desteklenmeyen stop bits değeri.")
        };
    }

    private static Handshake MapFlowControl(
        SerialFlowControl flowControl)
    {
        return flowControl switch
        {
            SerialFlowControl.None =>
                Handshake.None,

            SerialFlowControl.XOnXOff =>
                Handshake.XOnXOff,

            SerialFlowControl.RtsCts =>
                Handshake.RequestToSend,

            SerialFlowControl.RtsCtsXOnXOff =>
                Handshake.RequestToSendXOnXOff,

            _ => throw new ArgumentOutOfRangeException(
                nameof(flowControl),
                flowControl,
                "Desteklenmeyen flow control değeri.")
        };
    }
}