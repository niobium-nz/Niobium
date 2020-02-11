using System.Collections.Generic;

namespace Cod.Channel
{
    public class TableQueryResult<T>
    {
        public List<T> Data { get; set; }

        public ContinuationToken ContinuationToken { get; set; }

        public bool HasMore => this.ContinuationToken != null;
    }
}
