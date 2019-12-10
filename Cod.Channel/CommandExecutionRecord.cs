using System;

namespace Cod.Channel
{
    public class CommandExecutionRecord
    {
        public CommandExecutionEventArgs EventArgs { get; private set; }

        public DateTimeOffset Occurred { get; private set; }

        public CommandExecutionRecord(CommandExecutionEventArgs eventArgs)
        {
            this.EventArgs = eventArgs;
            this.Occurred = DateTimeOffset.UtcNow;
        }
    }
}
