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

        public bool IsBusinessThePartitionKey { get; set; }

        public static string BuildKey(Guid value) => value.ToString("N").ToUpperInvariant();

        public Guid GetBusiness()
            => this.IsBusinessThePartitionKey ? Guid.Parse(this.PartitionKey) : Guid.Parse(this.RowKey);

        public void SetBusiness(Guid value)
        {
            if (this.IsBusinessThePartitionKey) this.PartitionKey = BuildKey(value);
            else this.RowKey = BuildKey(value);
        }

        public Guid GetID()
            => this.IsBusinessThePartitionKey ? Guid.Parse(this.RowKey) : Guid.Parse(this.PartitionKey);

        public void SetID(Guid value)
        {
            if (this.IsBusinessThePartitionKey) this.RowKey = BuildKey(value);
            else this.PartitionKey = BuildKey(value);
        }

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
