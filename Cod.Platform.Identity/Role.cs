using System.Diagnostics.CodeAnalysis;

namespace Cod.Platform.Identity
{
    [method: SetsRequiredMembers]
    internal class Role() : ITrackable
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public required Guid Tenant { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public required Guid User { get; set; }

        [EntityKey(EntityKeyKind.Timestamp)]
        public DateTimeOffset? Timestamp { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string? ETag { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string? Roles { get; set; }

        public IEnumerable<string> GetRoles()
        {
            return Roles == null ? Enumerable.Empty<string>() : Roles.Split(",", StringSplitOptions.RemoveEmptyEntries);
        }

        public bool AssignRole(string role)
        {
            role = role.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(Roles))
            {
                Roles = role;
                return true;
            }
            else
            {
                List<string> roles = GetRoles().ToList();
                if (!roles.Contains(role))
                {
                    roles.Add(role);
                    Roles = string.Join(",", roles);
                    return true;
                }
            }

            return false;
        }

        public void RemoveRole(string role)
        {
            role = role.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(Roles))
            {
                return;
            }
            else
            {
                List<string> roles = GetRoles().ToList();
                if (roles.Contains(role))
                {
                    roles.Remove(role);
                    Roles = string.Join(",", roles);
                }
            }
        }

        public static string BuildPartitionKey(Guid tenant) => tenant.ToString();
        public static string BuildRowKey(Guid user) => user.ToString();
    }
}
