using System;

namespace Cod.Model
{
    public class Report : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public long Count { get; set; }

        public string Reference { get; set; }

        public static string BuildPartitionKey(ushort kind, ReportPeriod period, string group, ushort year, ushort sequence)
        {
            return $"{kind.ToString().PadLeft(2, '0')}|{(int)period}|{group.Trim()}|{year}|{sequence.ToString().PadLeft(4, '0')}";
        }

        public static string BuildPartitionKey(ushort kind, ReportPeriod period, string group, ushort year, DateTimeOffset time)
        {
            int sequence = period switch
            {
                ReportPeriod.Undefined => 0,
                ReportPeriod.Hourly => (time.DayOfYear * 24) + time.Hour,
                ReportPeriod.Daily => time.DayOfYear,
                ReportPeriod.Weekly => time.GetWeekOfYear(),
                ReportPeriod.Monthly => time.Month,
                ReportPeriod.Quarterly => (int)Math.Ceiling(time.Month / 4.0d),
                ReportPeriod.SemiAnnually => time.Month < 7 ? 1 : 2,
                ReportPeriod.Annually => 0,
                _ => throw new NotImplementedException(),
            };
            return BuildPartitionKey(kind, period, group, year, (ushort)sequence);
        }

        public static string BuildRowKey(string id)
        {
            return id is null ? throw new ArgumentNullException(nameof(id)) : id.Trim();
        }

        public int GetYear()
        {
            return int.Parse(PartitionKey.Substring(PartitionKey.Length - 9, 4));
        }

        public ReportPeriod GetPeriod()
        {
            return (ReportPeriod)int.Parse(PartitionKey.Substring(3, 1));
        }

        public ushort GetSequence()
        {
            return ushort.Parse(PartitionKey.Substring(PartitionKey.Length - 4, 4));
        }

        public ushort GetKind()
        {
            return ushort.Parse(PartitionKey.Substring(0, 2));
        }

        public string GetGroup()
        {
            return PartitionKey.Substring(5, PartitionKey.Length - 3 - 2 - 5 - 5);
        }

        public string GetID()
        {
            return RowKey.Trim();
        }
    }
}
