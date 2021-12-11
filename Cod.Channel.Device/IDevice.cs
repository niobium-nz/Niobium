using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Channel.Device
{
    public interface IDevice : IAsyncDisposable
    {
        DeviceConnectionStatus Status { get; }

        Task ConnectAsync();

        Task SendAsync(ITimestampable data);

        Task ReportPropertyChangesAsync(IReadOnlyDictionary<string, object> properties);
    }
}
