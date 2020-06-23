using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod
{
    public class User : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public bool Disabled { get; set; }

        public string FirstIP { get; set; }

        public string LastIP { get; set; }

        public string Roles { get; set; }

        public static string BuildPartitionKey(Guid value) => value.ToString("N").ToUpperInvariant().Substring(0, 8);

        public static string BuildRowKey(Guid value) => value.ToString("N").ToUpperInvariant();

        public Guid GetID()
            => Guid.Parse(this.RowKey);

        public void SetID(Guid value) => this.RowKey = BuildRowKey(value);

        public IEnumerable<string> GetRoles()
            => this.Roles == null ? Enumerable.Empty<string>() : this.Roles.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

        public void AddRole(string role)
        {
            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            role = role.Trim().ToUpperInvariant();
            if (String.IsNullOrEmpty(this.Roles))
            {
                this.Roles = role;
            }
            else
            {
                var roles = this.GetRoles().ToList();
                if (!roles.Contains(role))
                {
                    roles.Add(role);
                    this.Roles = String.Join(",", roles);
                }
            }
        }

        public void RemoveRole(string role)
        {
            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            role = role.Trim().ToUpperInvariant();
            if (String.IsNullOrEmpty(this.Roles))
            {
                return;
            }
            else
            {
                var roles = this.GetRoles().ToList();
                if (roles.Contains(role))
                {
                    roles.Remove(role);
                    this.Roles = String.Join(",", roles);
                }
            }
        }

        public static string GetImpedementID(StorageKey key) => key.RowKey;
    }
}
