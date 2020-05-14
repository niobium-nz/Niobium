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

        public static User Reverse(User input)
            => new User
            {
                IsBusinessThePartitionKey = !input.IsBusinessThePartitionKey,
                Created = input.Created,
                Disabled = input.Disabled,
                FirstIP = input.FirstIP,
                LastIP = input.LastIP,
                PartitionKey = input.RowKey,
                RowKey = input.PartitionKey,
                Roles = input.Roles,
            };

        public User Reverse() => Reverse(this);

        public Guid GetBusiness()
            => this.IsBusinessThePartitionKey ? Guid.Parse(this.PartitionKey) : Guid.Parse(this.RowKey);

        public void SetBusiness(Guid value)
        {
            if (this.IsBusinessThePartitionKey) this.PartitionKey = value.ToString("N").ToUpperInvariant();
            else this.RowKey = value.ToString("N").ToUpperInvariant();
        }

        public Guid GetID()
            => this.IsBusinessThePartitionKey ? Guid.Parse(this.RowKey) : Guid.Parse(this.PartitionKey);

        public void SetID(Guid value)
        {
            if (this.IsBusinessThePartitionKey) this.RowKey = value.ToString("N").ToUpperInvariant();
            else this.PartitionKey = value.ToString("N").ToUpperInvariant();
        }

        public IEnumerable<string> GetRoles()
            => this.Roles == null ? Enumerable.Empty<string>() : this.Roles.Split(',');
    }
}
