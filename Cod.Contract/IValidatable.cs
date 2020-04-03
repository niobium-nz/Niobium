using System.Collections.Generic;

namespace Cod
{
    public interface IValidatable
    {
        IReadOnlyDictionary<string, string> Validate();
    }
}
