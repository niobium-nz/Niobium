using System;

namespace Cod.Channel
{
    class BusyState : IDisposable
    {
        private ICommander commander;
        private string group;
        private string name;

        public BusyState(ICommander commander, string group, string name)
        {
            this.commander = commander;
            this.group = group;
            this.name = name;
        }

        public void Dispose()
        {
            if (this.commander != null)
            {
                this.commander.UnsetBusy(this.group, this.name);
                this.commander = null;
                this.group = null;
                this.name = null;
            }
        }
    }
}
