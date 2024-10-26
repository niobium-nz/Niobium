namespace Cod
{
    public class ResourcePermission
    {
        public ResourceType Type { get; set; }

        public string Resource { get; set; }

        public string Partition { get; set; }

        public string Scope { get; set; }

        public IEnumerable<string> Entitlements { get; set; }
    }
}
