using System.Collections.Generic;

namespace Cod
{
    public interface IValidateable
    {
        IReadOnlyDictionary<string, string> Validate();
    }
}
