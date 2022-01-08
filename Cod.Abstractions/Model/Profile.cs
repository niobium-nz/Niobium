using System;

namespace Cod.Model
{
    public class Profile : IEntity
    {
        public const string ProfileBlobContainer = "profile";

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public string Phone { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public static string BuildPartitionKey(Guid business, Guid user)
            => $"{business.ToKey()}|{user.ToKey()}";

        public static bool TryParse(string partitionKey, out Guid business, out Guid user)
        {
            business = default;
            user = default;
            if (partitionKey is null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            var splited = partitionKey.Split('|');
            if (splited.Length == 2)
            {
                return Guid.TryParse(splited[0], out business) && Guid.TryParse(splited[1], out user);
            }

            return false;
        }

        public Guid GetBusiness()
        {
            if (Profile.TryParse(this.PartitionKey, out var business, out var _))
            {
                return business;
            }

            throw new NotSupportedException();
        }

        public Guid GetUser()
        {
            if (Profile.TryParse(this.PartitionKey, out var _, out var user))
            {
                return user;
            }

            throw new NotSupportedException();
        }
    }
}
