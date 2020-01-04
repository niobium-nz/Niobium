namespace Cod.Channel
{
    public abstract class ChannelDomain<T> : GenericDomain<T>, IChannelDomain<T> where T : IEntity
    {
        public T Entity { get; private set; }

        public override string PartitionKey => this.Entity.PartitionKey;

        public override string RowKey => this.Entity.RowKey;

        protected override void OnInitialize(T entity) => this.Entity = entity;
    }
}
