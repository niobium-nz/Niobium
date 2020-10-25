using Cod.Model;
using System;

namespace Cod
{
    public class SMSJob : Job
    {
        public bool IsSupport() => this.PartitionKey.StartsWith(JobKinds.SMS, StringComparison.CurrentCultureIgnoreCase);

        public string GetCorrelation() => this.RowKey;

        public static string BuildPartitionKey(string user) => $"{JobKinds.SMS}-{user.ToUpperInvariant()}";

        public static string BuildRowKey(string correlation) => correlation.Trim();
    }
}
