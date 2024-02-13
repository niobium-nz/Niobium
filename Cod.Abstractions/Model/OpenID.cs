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
            => user.ToString("N").ToUpperInvariant();

        public static string BuildRowKey(OpenIDKind kind, string identifier = null)
            => BuildRowKey((int)kind, identifier);

        public static string BuildRowKey(int kind, string identifier = null)
        {
            identifier ??= String.Empty;

            return $"{BuildRowKeyStart(kind)}|{identifier.Trim()}";
        }

        public static string BuildRowKey(OpenIDKind kind, string app, string identifier = null)
            => BuildRowKey((int)kind, app, identifier);

        public static string BuildRowKey(int kind, string app, string identifier = null)
        {
            identifier ??= String.Empty;
            app ??= String.Empty;
            return $"{BuildRowKeyStart(kind, app)}{identifier.Trim()}";
        }

        public static string BuildRowKeyStart(OpenIDKind kind) => BuildRowKeyStart((int)kind);

        public static string BuildRowKeyEnd(OpenIDKind kind) => $"{BuildRowKeyStart(kind)}~";

        public static string BuildRowKeyStart(int kind) => $"{kind}|";

        public static string BuildRowKeyEnd(int kind) => $"{BuildRowKeyStart(kind)}~";

        public static string BuildRowKeyStart(OpenIDKind kind, string app) => BuildRowKeyStart((int)kind, app);

        public static string BuildRowKeyEnd(OpenIDKind kind, string app) => $"{BuildRowKeyStart(kind, app)}~";

        public static string BuildRowKeyStart(int kind, string app) => $"{BuildRowKeyStart(kind)}{app.Trim()}|";

        public static string BuildRowKeyEnd(int kind, string app) => $"{BuildRowKeyStart(kind, app)}~";

        public Guid GetUser() => Guid.Parse(this.PartitionKey);

        public string GetApp()
        {
            var parts = this.RowKey.Split('|');
            return parts.Length == 3 ? parts[1] : null;
        }

        public int GetKind() => Int32.Parse(this.RowKey.Split('|')[0]);

        public bool IsKindOf(OpenIDKind type) => this.IsKindOf((int)type);

        public bool IsKindOf(int type) => this.GetKind() == type;
    }
}
