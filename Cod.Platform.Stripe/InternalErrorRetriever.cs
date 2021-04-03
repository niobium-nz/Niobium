using System;
using System.Collections.Generic;
using System.Text;

namespace Cod.Platform
{
    internal class InternalErrorRetriever : IErrorRetriever
    {
        public bool TryGet(string key, out string value) => R.TryGet(key, out value);
    }
}
