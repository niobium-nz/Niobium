using System;

namespace Cod.Channel.Device
{
    public class CloudToDeviceMessage
    {
        public string CorrelationID { get; set; }

        public DateTimeOffset Enqueued { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset Valids { get; set; }

        public DateTimeOffset Expires { get; set; }

        public uint DeliveryCount { get; set; }

        public string JSONBody { get; set; }
    }
}
