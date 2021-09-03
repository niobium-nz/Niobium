using System;

namespace Cod
{
    public interface ITimestampable
    {
        void SetTimestamp(DateTimeOffset stamp);
    }
}
