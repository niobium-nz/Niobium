using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public interface ILoggerSite
    {
        ILogger Logger { get; }
    }
}
