using SerialDebugStudio.Domain.Enums;

namespace SerialDebugStudio.Domain.Models;

public sealed record ConnectionInfo(
    TransportKind TransportKind,
    ConnectionState State,
    string Endpoint,
    DateTimeOffset? ConnectedAtUtc = null,
    DateTimeOffset? DisconnectedAtUtc = null,
    string? ErrorMessage = null);