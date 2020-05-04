using System;

namespace Cod.Platform
{
    public class ApplePushNotification
    {
        public bool Background { get; set; }

        public Guid ID { get; set; }

        public DateTimeOffset Expires { get; set; }

        public string Topic { get; set; }

        public object Message { get; set; }
    }
}
