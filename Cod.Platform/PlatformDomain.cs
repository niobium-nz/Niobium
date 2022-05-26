namespace Cod.Platform
{
    public abstract class PlatformDomain<T> : GenericDomain<T>, IPlatformDomain<T> where T : IEntity
    {
        private readonly Lazy<IRepository<T>> repository;
        private T cache;
        private Func<Task<T>> getEntity;
        private string partitionKey;
        private string rowKey;

        public PlatformDomain(Lazy<IRepository<T>> repository) => this.repository = repository;

        public override string PartitionKey => this.partitionKey;

        public override string RowKey => this.rowKey;

        protected IRepository<T> Repository => this.repository.Value;

        public async Task<T> GetEntityAsync()
        {
            if (this.cache == null)
            {
                this.cache = await this.getEntity();
            }
            return this.cache;
        }

        public IDomain<T> Initialize(string partitionKey, string rowKey)
        {
            if (!this.Initialized)
            {
                this.getEntity = async () => await this.Repository.GetAsync(partitionKey, rowKey);
                this.partitionKey = partitionKey;
                this.rowKey = rowKey;
            }
            this.Initialized = true;
            return this;
        }

        public async Task<PlatformDomain<T>> ReloadAsync()
        {
            if (!this.Initialized)
            {
                throw new NotSupportedException();
            }
            this.cache = await this.Repository.GetAsync(this.partitionKey, this.rowKey);
            return this;
        }

        protected override void OnInitialize(T entity)
        {
            if (!this.Initialized)
            {
                this.getEntity = () => Task.FromResult(entity);
                this.partitionKey = entity.PartitionKey;
                this.rowKey = entity.RowKey;
            }
            this.Initialized = true;
        }

        protected async Task SaveEntityAsync(bool force = false)
            => await this.SaveEntityAsync(new[] { await this.GetEntityAsync() }, force);

        protected async Task SaveEntityAsync(IEnumerable<T> model, bool force = false)
        {
            if (model == null || !model.Any())
            {
                return;
            }
            if (force)
            {
                await this.Repository.CreateOrUpdateAsync(model);
            }
            else
            {
                var groups = model.GroupBy(m => m.ETag == null);

                foreach (var group in groups)
                {
                    if (group.Key)
                    {
                        await this.Repository.CreateAsync(group);
                    }
                    else
                    {
                        await this.Repository.UpdateAsync(group);
                    }
                }
            }

            if (this.Initialized)
            {
                var c = model.SingleOrDefault(m => m.PartitionKey == this.PartitionKey && m.RowKey == this.rowKey);
                if (c != null)
                {
                    this.cache = c;
                }
            }
        }
    }
}