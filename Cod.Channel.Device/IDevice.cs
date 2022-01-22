using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cod.Channel.Device
{
    public interface IDevice : IAsyncDisposable
    {
        DeviceConnectionStatus Status { get; }

        Task ConnectAsync();

        Task ConnectAsync(CancellationToken cancellationToken);

        Task SendAsync(ITimestampable data);

        Task SendAsync(ITimestampable data, CancellationToken cancellationToken);

        Task ReportPropertyChangesAsync(IReadOnlyDictionary<string, object> properties);
    }
}
