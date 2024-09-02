﻿using Cod.Platform.Tenant;

namespace Cod.Platform.Identity
{
    public class Login : ITrackable
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

        public string Credentials { get; set; }

        public Guid User { get; set; }

        public Guid Business { get; set; }

        public static string BuildPartitionKey(OpenIDKind kind)
        {
            return BuildPartitionKey((int)kind);
        }

        public static string BuildPartitionKey(AuthenticationKind kind)
        {
            return BuildPartitionKey((int)kind);
        }

        public static string BuildPartitionKey(int kind)
        {
            return BuildPartitionKey(kind, default);
        }

        public static string BuildPartitionKey(OpenIDKind kind, string app)
        {
            return BuildPartitionKey((int)kind, app);
        }

        public static string BuildPartitionKey(AuthenticationKind kind, string app)
        {
            return BuildPartitionKey((int)kind, app);
        }

        public static string BuildPartitionKey(int kind, string app)
        {
            app ??= string.Empty;
            return $"{kind}|{app.Trim()}";
        }

        public static string BuildRowKey(string identity)
        {
            return identity is null ? throw new ArgumentNullException(nameof(identity)) : identity.Trim();
        }

        public string GetIdentity()
        {
            return RowKey.Trim();
        }

        public int GetKind()
        {
            return int.Parse(PartitionKey.Split('|')[0]);
        }

        public bool IsKindOf(OpenIDKind type)
        {
            return IsKindOf((int)type);
        }

        public bool IsKindOf(int type)
        {
            return GetKind() == type;
        }
    }
}
