using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public static class Logger
    {
        private const string KEY = "logger";

        public static void Register(ILogger logger) => CallContext<ILogger>.SetData(KEY, logger);

        public static ILogger Instance => CallContext<ILogger>.GetData(KEY);
    }
}
