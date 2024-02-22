using System;

namespace Cod
{
    public interface ITimestampable
    {
        DateTimeOffset Timestamp { get; set; }
    }
}
