namespace Cod
{
    public abstract class GenericDomain<T> : IDomain<T> where T : IEntity
    {
        public bool Initialized { get; protected set; }

        public abstract string PartitionKey { get; }

        public abstract string RowKey { get; }

        public IDomain<T> Initialize(T entity)
        {
            if (!Initialized)
            {
                OnInitialize(entity);
            }
            Initialized = true;
            return this;
        }

        protected virtual void OnInitialize(T entity) { }
    }
}
