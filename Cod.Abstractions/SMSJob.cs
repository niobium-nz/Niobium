using Cod.Model;

namespace Cod
{
    public class SMSJob : Job
    {
        public bool Match() => this.PartitionKey.StartsWith(JobKinds.SMS);

        public string GetCorrelation() => this.RowKey;

        public static string BuildPartitionKey(string user) => $"{JobKinds.SMS}-{user.ToUpper()}";

        public static string BuildRowKey(string correlation) => correlation.Trim();
    }
}
