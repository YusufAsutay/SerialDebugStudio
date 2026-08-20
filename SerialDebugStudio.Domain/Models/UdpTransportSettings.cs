namespace SerialDebugStudio.Domain.Models;

public sealed record UdpTransportSettings
{
    public string LocalIpAddress
    {
        get;
        init;
    } = "0.0.0.0";

    public int LocalPort
    {
        get;
        init;
    } = 5000;

    public string RemoteIpAddress
    {
        get;
        init;
    } = "127.0.0.1";

    public int RemotePort
    {
        get;
        init;
    } = 5001;
}