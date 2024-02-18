using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod.Model
{
    public class User : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public bool Disabled { get; set; }

        public string FirstIP { get; set; }

        public string LastIP { get; set; }

        public string Roles { get; set; }

        public static string BuildPartitionKey(Guid value)
        {
            return value.ToString("N").ToUpperInvariant().Substring(0, 8);
        }

        public static string BuildRowKey(Guid value)
        {
            return value.ToString("N").ToUpperInvariant();
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
