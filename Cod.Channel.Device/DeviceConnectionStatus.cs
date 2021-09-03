namespace Cod.Channel.IoT
{
    public enum DeviceConnectionStatus : int
    {
        Disconnected = 0,
        Connected = 1,
        Disconnected_Retrying = 2,
        Disabled = 3
    }
}
