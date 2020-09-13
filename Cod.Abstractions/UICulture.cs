using System.Globalization;
using System.Threading;

namespace Cod
{
    public static class UICulture
    {
        public static CultureInfo Default = new CultureInfo("en-US");
        private static readonly AsyncLocal<CultureInfo> culture = new AsyncLocal<CultureInfo>();

        public static void Register(CultureInfo value) => culture.Value = value;

        public static CultureInfo Current => culture.Value ?? Default;
    }
}
