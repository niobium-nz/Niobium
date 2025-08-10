namespace Cod.Channel
{
    public class LoadCommandParameter
    {
        public LoadCommandParameter(string partitionKey)
        {
            PartitionKeyStart = partitionKey;
            PartitionKeyEnd = partitionKey;
        }

        public LoadCommandParameter(string partitionKey, string rowKey)
            : this(partitionKey)
        {
            RowKeyStart = rowKey;
            RowKeyEnd = rowKey;
        }

        public string PartitionKeyStart { get; set; }

        public string PartitionKeyEnd { get; set; }

        public string? RowKeyStart { get; set; }

        public string? RowKeyEnd { get; set; }

        public bool Force { get; set; }
    }
}
