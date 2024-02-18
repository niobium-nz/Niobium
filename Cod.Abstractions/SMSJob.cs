using Cod.Model;

namespace Cod
{
    public class SMSJob : Job
    {
        public bool Match()
        {
            return PartitionKey.StartsWith(JobKinds.SMS);
        }

        public string GetCorrelation()
        {
            return RowKey;
        }

        public static string BuildPartitionKey(string user)
        {
            return $"{JobKinds.SMS}-{user.ToUpper()}";
        }

        public static string BuildRowKey(string correlation)
        {
            return correlation.Trim();
        }
    }
}
