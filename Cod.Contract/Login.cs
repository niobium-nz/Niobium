using System;

namespace Cod
{
    public class Login : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string Credentials { get; set; }

        public Guid User { get; set; }

        public static string BuildPartitionKey(OpenIDKind kind)
            => BuildPartitionKey((int)kind);

        public static string BuildPartitionKey(int kind)
            => BuildPartitionKey(kind, default);

        public static string BuildPartitionKey(OpenIDKind kind, string app)
            => BuildPartitionKey((int)kind, app);

        public static string BuildPartitionKey(int kind, string app)
        {
            if (app is null)
            {
                app = String.Empty;
            }
            return $"{kind}|{app.Trim()}";
        }

        public static string BuildRowKey(string identity)
        {
            if (identity is null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            return identity.Trim();
        }

        public string GetIdentity() => this.RowKey.Trim();

        public int GetKind() => Int32.Parse(this.PartitionKey.Split('|')[0]);

        public bool IsKindOf(OpenIDKind type) => this.IsKindOf((int)type);

        public bool IsKindOf(int type) => this.GetKind() == type;
    }
}
