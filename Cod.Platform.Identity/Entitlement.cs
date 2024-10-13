using Cod.Identity;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Cod.Platform.Identity
{
    [method: SetsRequiredMembers]
#pragma warning disable CS8618
    internal class Entitlement() : ITrackable
#pragma warning restore CS8618
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
            var list = Entitlements.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in list)
            {
                var parts = item.Split('@', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var resource = parts[0];
                    parts = parts[1].Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var type = parts[0];
                        var permission = parts[1];

                        if (int.TryParse(type, out var resourceType))
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
            var result = new List<string>();
            foreach (var item in input)
            {
                var sb = new StringBuilder();
                sb.Append(item.Resource);
                sb.Append('@');
                sb.Append((int)item.Type);
                sb.Append(':');
                sb.Append(item.Permission);
                result.Add(sb.ToString());
            }

            Entitlements = string.Join(",", result);
        }

        public static string BuildPartitionKey(Guid tenant) => tenant.ToString();
        public static string BuildRowKey(string role) => role.Trim();
    }
}
