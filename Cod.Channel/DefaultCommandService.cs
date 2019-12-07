using System.Collections.Generic;
using System.Linq;

namespace Cod.Channel
{
    internal class DefaultCommandService : ICommandService
    {
        private readonly IEnumerable<ICommand> commands;

        public DefaultCommandService(IEnumerable<ICommand> commands) => this.commands = commands;

        public ICommand Get(string commandID) => this.commands.SingleOrDefault(c => c.CommandID == commandID);
    }
}
