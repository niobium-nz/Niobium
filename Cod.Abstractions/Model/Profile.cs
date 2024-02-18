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

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public static string BuildPartitionKey(Guid business, Guid user)
        {
            return $"{business.ToKey()}|{user.ToKey()}";
        }

        public static bool TryParse(string partitionKey, out Guid business, out Guid user)
        {
            business = default;
            user = default;
            if (partitionKey is null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            string[] splited = partitionKey.Split('|');
            return splited.Length == 2 && Guid.TryParse(splited[0], out business) && Guid.TryParse(splited[1], out user);
        }

        public Guid GetBusiness()
        {
            return Profile.TryParse(PartitionKey, out Guid business, out Guid _) ? business : throw new NotSupportedException();
        }

        public Guid GetUser()
        {
            return Profile.TryParse(PartitionKey, out Guid _, out Guid user) ? user : throw new NotSupportedException();
        }
    }
}
