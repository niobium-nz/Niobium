using System;

namespace Cod.Model
{
    public class Report : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public long Count { get; set; }

        public static string BuildPartitionKey(ushort year, ReportPeriod period, ushort sequence)
        {
            return $"{year}|{(int)period}|{sequence.ToString().PadLeft(4, '0')}";
        }

        public static string BuildPartitionKey(ushort year, ReportPeriod period, DateTimeOffset time)
        {
            var sequence = period switch
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
            return BuildPartitionKey(year, period, (ushort)sequence);
        }

        public static string BuildRowKey(string id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return id.Trim();
        }

        public int GetYear() => Int32.Parse(this.PartitionKey.Substring(0, 4));

        public ReportPeriod GetPeriod() => (ReportPeriod)Int32.Parse(this.PartitionKey.Substring(4, 1));

        public ushort GetSequence() => UInt16.Parse(this.PartitionKey.Substring(5, 4));

        public long GetID() => Int64.Parse(this.RowKey);
    }
}
