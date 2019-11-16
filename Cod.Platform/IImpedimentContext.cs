using Cod.Contract;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class IImpedimentContext<T> where T : IImpedable
    {
        public int Cause { get; set; }

        public string PolicyInput { get; set; }

        public string Category { get; set; }

        public T Entity { get; set; }

        public ILogger Log { get; set; }
    }
}