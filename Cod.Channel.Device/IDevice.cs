using System;
using System.Threading.Tasks;

namespace Cod.Channel.IoT
{
    public interface IDevice : IAsyncDisposable
    {
        DeviceConnectionStatus Status { get; }

        Task ConnectAsync();

        Task SendAsync(ITimestampable data);
    }
}
