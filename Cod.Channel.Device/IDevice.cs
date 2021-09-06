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

        Task SetDesiredPropertyUpdateCallbackAsync(DeviceDesiredPropertyUpdateCallback callback);

        Task UpdateReportedPropertiesAsync(IReadOnlyDictionary<string, object> reportedProperties);
    }

    public delegate Task DeviceDesiredPropertyUpdateCallback(IReadOnlyDictionary<string, object> desiredProperties);
}
