using System.Collections.Generic;

namespace Cod.Contract
{
    public class InternalError
    {
        public static readonly IDictionary<int, string> Messages;

        static InternalError() => Messages = new Dictionary<int, string>();
    }
}
