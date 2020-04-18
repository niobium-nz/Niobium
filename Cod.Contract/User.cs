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

        public string Businesses { get; set; }

        public static string BuildPartitionKey(Guid user)
            => BuildRowKey(user).Substring(0, 3);

        public static string BuildRowKey(Guid user)
            => user.ToString("N").ToUpperInvariant();

        public IEnumerable<Guid> GetBusinesses()
            => this.Businesses == null ? Enumerable.Empty<Guid>() : this.Businesses.Split(',').Select(p => Guid.Parse(p));

        public void AddToBusiness(Guid business)
        {
            var str = business.ToString("N").ToUpperInvariant();
            if (String.IsNullOrWhiteSpace(this.Businesses))
            {
                this.Businesses = str;
            }
            else if (!this.Businesses.Contains(str))
            {
                this.Businesses += $"{str},";
            }
        }

        public void RemoveFromBusiness(Guid business)
        {
            var businesses = GetBusinesses().ToList();
            if (businesses.Contains(business))
            {
                businesses.Remove(business);
                var strs = businesses.Select(b => b.ToString("N").ToUpperInvariant());
                this.Businesses = String.Join(",", strs);
            }
        }
    }
}
