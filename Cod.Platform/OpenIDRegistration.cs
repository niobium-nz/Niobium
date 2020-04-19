namespace Cod.Platform
{
    public class OpenIDRegistration
    {
        public string Account { get; set; }

        public int Kind { get; set; }

        public string App { get; set; }

        public string Identity { get; set; }

        public bool OverrideIfExists { get; set; }

        public string OffsetPrefix { get; set; }
    }
}
