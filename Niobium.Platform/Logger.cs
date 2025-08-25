using Microsoft.Extensions.Logging;

namespace Niobium.Platform
{
    public static class Logger
    {
        private static readonly AsyncLocal<ILogger> logger = new();

        public static void Register(ILogger value)
        {
            logger.Value = value;
        }

        public static ILogger? Instance => logger.Value;
    }
}
