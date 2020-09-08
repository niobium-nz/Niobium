using System.Globalization;
using System.Threading;

namespace Cod
{
    public static class UICulture
    {
        private static CultureInfo defaultCulture = new CultureInfo("en-US");
        private static readonly AsyncLocal<CultureInfo> culture = new AsyncLocal<CultureInfo>();

        public static void Initialize(CultureInfo value) => defaultCulture = value;

        public static void Register(CultureInfo value) => culture.Value = value;

        public static CultureInfo Current => culture.Value ?? defaultCulture;
    }
}
