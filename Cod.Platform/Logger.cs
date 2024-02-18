using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public static class Logger
    {
        private static readonly AsyncLocal<ILogger> logger = new();

        internal static void Register(ILogger value)
        {
            logger.Value = value;
        }

        public static ILogger Instance => logger.Value;
    }
}
