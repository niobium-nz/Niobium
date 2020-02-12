using System;

namespace Cod.Channel
{
    public class CommandExecutionEventArgs : EventArgs, ICloneable
    {
        public CommandExecutionEventArgs(CommandID id, object parameter)
        {
            this.CommandID = id;
            this.ExecutionID = Guid.NewGuid();
            this.Parameter = parameter;
        }

        public CommandID CommandID { get; private set; }

        public Guid ExecutionID { get; private set; }

        public object Parameter { get; private set; }

        public OperationResult Result { get; set; }

        public object Clone() => new CommandExecutionEventArgs(this.CommandID, this.Parameter)
        {
            ExecutionID = this.ExecutionID,
            Result = this.Result,
        };
    }
}
