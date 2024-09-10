namespace Cod.Platform.Identity
{
    public class EntitlementDescription
    {
        public required ResourceType Type { get; set; }

        public required string Resource { get; set; }

        public required string Permission { get; set; }
    }
}
