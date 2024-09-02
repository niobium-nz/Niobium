namespace Cod.Platform.Identity
{
    public class User : ITrackable
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public string PartitionKey { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public string RowKey { get; set; }

        [EntityKey(EntityKeyKind.Timestamp)]
        public DateTimeOffset? Timestamp { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string ETag { get; set; }

        public DateTimeOffset? Created { get; set; }

        public bool Disabled { get; set; }

        public string FirstIP { get; set; }

        public string LastIP { get; set; }

        public string Roles { get; set; }

        public static string BuildPartitionKey(Guid value)
        {
            if (CodSettings.UseLegacyGuidFormat)
            {
                return value.ToString("N").ToUpperInvariant()[..8];
            }
            else
            {
                return value.ToString()[..8];
            }
        }

        public static string BuildRowKey(Guid value)
        {
            if (CodSettings.UseLegacyGuidFormat)
            {
                return value.ToString("N").ToUpperInvariant();
            }
            else
            {
                return value.ToString();
            }
        }

        public Guid GetID()
        {
            return Guid.Parse(RowKey);
        }

        public void SetID(Guid value)
        {
            RowKey = BuildRowKey(value);
        }

        public IEnumerable<string> GetRoles()
        {
            return Roles == null ? Enumerable.Empty<string>() : Roles.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool AddRole(string role)
        {
            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }

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
            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }

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

        public static string GetImpedementID(StorageKey key)
        {
            return key.RowKey;
        }
    }
}
