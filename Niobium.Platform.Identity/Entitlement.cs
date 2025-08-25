using Niobium.Identity;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Niobium.Platform.Identity
{
#pragma warning disable CS8618
    [method: SetsRequiredMembers]
    internal sealed class Entitlement() : ITrackable
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public required Guid Tenant { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public required string Role { get; set; }

        [EntityKey(EntityKeyKind.Timestamp)]
        public DateTimeOffset? Timestamp { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string? ETag { get; set; }

        public DateTimeOffset? Created { get; set; }

        public required string Entitlements { get; set; }

        public IEnumerable<EntitlementDescription> GetEntitlements()
        {
            List<EntitlementDescription> result = [];
            string[] list = Entitlements.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in list)
            {
                string[] parts = item.Split('@', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    string resource = parts[0];
                    parts = parts[1].Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string type = parts[0];
                        string permission = parts[1];

                        if (int.TryParse(type, out int resourceType))
                        {
                            result.Add(new EntitlementDescription
                            {
                                Permission = permission,
                                Resource = resource,
                                Type = (ResourceType)resourceType,
                            });
                        }
                    }
                }
            }

            return result;
        }

        public void SetEntitlements(IEnumerable<EntitlementDescription> input)
        {
            List<string> result = [];
            foreach (EntitlementDescription item in input)
            {
                StringBuilder sb = new();
                sb.Append(item.Resource);
                sb.Append('@');
                sb.Append((int)item.Type);
                sb.Append(':');
                sb.Append(item.Permission);
                result.Add(sb.ToString());
            }

            Entitlements = string.Join(",", result);
        }

        public static string BuildPartitionKey(Guid tenant)
        {
            return tenant.ToString();
        }

        public static string BuildRowKey(string role)
        {
            return role.Trim();
        }
    }
#pragma warning restore CS8618
}
