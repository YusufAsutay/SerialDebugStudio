namespace SerialDebugStudio.Domain.Enums;

public enum ChecksumAlgorithm
{
    None,
    Sum8,
    Crc8OpenSafety,
    Crc8Smbus,
    Crc16Modbus,
    Crc16Gsm
}