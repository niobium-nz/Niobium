using System;
using System.Collections.Generic;

namespace Cod.Channel
{
    public class CommandExecutionEventArgs : EventArgs
    {
        public CommandExecutionEventArgs(CommandID id, string message, IEnumerable<object> args)
        {
            this.CommandID = id;
            this.ExecutionID = Guid.NewGuid();
            this.Message = message;
            this.Args = args;
        }

        public CommandID CommandID { get; set; }

        public Guid ExecutionID { get; private set; }

        public string Message { get; }

        public IEnumerable<object> Args { get; }
    }
}
