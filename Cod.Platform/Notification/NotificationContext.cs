using System;

namespace Cod.Platform
{
    public class NotificationContext
    {
        public NotificationContext(
            int kind,
            string app,
            Guid user,
            string identity)
        {
            this.Kind = kind;
            this.App = app;
            this.User = user;
            this.Identity = identity;
        }

        public int Kind { get; private set; }

        public string App { get; private set; }

        public Guid User { get; private set; }

        public string Identity { get; private set; }
    }
}
