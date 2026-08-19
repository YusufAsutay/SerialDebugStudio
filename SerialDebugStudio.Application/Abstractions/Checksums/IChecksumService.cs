using SerialDebugStudio.Domain.Enums;

namespace SerialDebugStudio.Application.Abstractions.Checksums;

public interface IChecksumService
{
    byte[] Calculate(
        ReadOnlySpan<byte> data,
        ChecksumAlgorithm algorithm);

    byte[] Append(
        ReadOnlySpan<byte> data,
        ChecksumAlgorithm algorithm);
}