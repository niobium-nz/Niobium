using System;

namespace Cod.Model
{
    public class OpenID : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string Identity { get; set; }

        public static string BuildPartitionKey(Guid user)
        {
            return user.ToString("N").ToUpperInvariant();
        }

        public static string BuildRowKey(OpenIDKind kind, string identifier = null)
        {
            return BuildRowKey((int)kind, identifier);
        }

        public static string BuildRowKey(int kind, string identifier = null)
        {
            identifier ??= string.Empty;

            return $"{BuildRowKeyStart(kind)}|{identifier.Trim()}";
        }

        public static string BuildRowKey(OpenIDKind kind, string app, string identifier = null)
        {
            return BuildRowKey((int)kind, app, identifier);
        }

        public static string BuildRowKey(int kind, string app, string identifier = null)
        {
            identifier ??= string.Empty;
            app ??= string.Empty;
            return $"{BuildRowKeyStart(kind, app)}{identifier.Trim()}";
        }

        public static string BuildRowKeyStart(OpenIDKind kind)
        {
            return BuildRowKeyStart((int)kind);
        }

        public static string BuildRowKeyEnd(OpenIDKind kind)
        {
            return $"{BuildRowKeyStart(kind)}~";
        }

        public static string BuildRowKeyStart(int kind)
        {
            return $"{kind}|";
        }

        public static string BuildRowKeyEnd(int kind)
        {
            return $"{BuildRowKeyStart(kind)}~";
        }

        public static string BuildRowKeyStart(OpenIDKind kind, string app)
        {
            return BuildRowKeyStart((int)kind, app);
        }

        public static string BuildRowKeyEnd(OpenIDKind kind, string app)
        {
            return $"{BuildRowKeyStart(kind, app)}~";
        }

        public static string BuildRowKeyStart(int kind, string app)
        {
            return $"{BuildRowKeyStart(kind)}{app.Trim()}|";
        }

        public static string BuildRowKeyEnd(int kind, string app)
        {
            return $"{BuildRowKeyStart(kind, app)}~";
        }

        public Guid GetUser()
        {
            return Guid.Parse(PartitionKey);
        }

        public string GetApp()
        {
            string[] parts = RowKey.Split('|');
            return parts.Length == 3 ? parts[1] : null;
        }

        public int GetKind()
        {
            return int.Parse(RowKey.Split('|')[0]);
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
