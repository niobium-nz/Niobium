using System.Collections.Generic;

namespace Cod
{
    public class ChangeAssignmentParameter
    {
        public StorageKey Key { get; set; }

        public IEnumerable<StorageKey> Added { get; set; }

        public IEnumerable<StorageKey> Removed { get; set; }
    }
}
