using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod
{
    public static class EnumHelper
    {
        public static IEnumerable<T> ForEach<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
}
