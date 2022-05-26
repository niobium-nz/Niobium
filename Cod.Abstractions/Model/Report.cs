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

        public string Reference { get; set; }

        public static string BuildPartitionKey(ushort kind, ReportPeriod period, string group, ushort year, ushort sequence) => $"{kind.ToString().PadLeft(2, '0')}|{(int)period}|{group.Trim()}|{year}|{sequence.ToString().PadLeft(4, '0')}";

        public static string BuildPartitionKey(ushort kind, ReportPeriod period, string group, ushort year, DateTimeOffset time)
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
            return BuildPartitionKey(kind, period, group, year, (ushort)sequence);
        }

        public static string BuildRowKey(string id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return id.Trim();
        }

        public int GetYear() => Int32.Parse(this.PartitionKey.Substring(this.PartitionKey.Length - 8, 4));

        public ReportPeriod GetPeriod() => (ReportPeriod)Int32.Parse(this.PartitionKey.Substring(2, 1));

        public ushort GetSequence() => UInt16.Parse(this.PartitionKey.Substring(this.PartitionKey.Length - 4, 4));

        public ushort GetKind() => UInt16.Parse(this.PartitionKey.Substring(0, 2));

        public string GetGroup() => this.PartitionKey.Substring(3, this.PartitionKey.Length - 2 - 1 - 4 - 4);

        public string GetID() => this.RowKey.Trim();
    }
}
