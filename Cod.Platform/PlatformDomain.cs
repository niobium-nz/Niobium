using Cod.Platform.Database;

namespace Cod.Platform
{
    public abstract class PlatformDomain<T> : GenericDomain<T>, IPlatformDomain<T> where T : IEntity
    {
        private readonly Lazy<IRepository<T>> repository;
        private T cache;
        private Func<Task<T>> getEntity;
        private string partitionKey;
        private string rowKey;

        public PlatformDomain(Lazy<IRepository<T>> repository)
        {
            this.repository = repository;
        }

        public override string PartitionKey => partitionKey;

        public override string RowKey => rowKey;

        protected IRepository<T> Repository => repository.Value;

        public async Task<T> GetEntityAsync()
        {
            cache ??= await getEntity();
            return cache;
        }

        public IDomain<T> Initialize(string partitionKey, string rowKey)
        {
            if (!Initialized)
            {
                getEntity = async () => await Repository.RetrieveAsync(partitionKey, rowKey);
                this.partitionKey = partitionKey;
                this.rowKey = rowKey;
            }
            Initialized = true;
            return this;
        }

        public async Task<PlatformDomain<T>> ReloadAsync()
        {
            if (!Initialized)
            {
                throw new NotSupportedException();
            }
            cache = await Repository.RetrieveAsync(partitionKey, rowKey);
            return this;
        }

        protected override void OnInitialize(T entity)
        {
            if (!Initialized)
            {
                getEntity = () => Task.FromResult(entity);
                partitionKey = entity.PartitionKey;
                rowKey = entity.RowKey;
            }
            Initialized = true;
        }

        protected async Task SaveEntityAsync(bool force = false)
        {
            await SaveEntityAsync(new[] { await GetEntityAsync() }, force);
        }

        protected async Task SaveEntityAsync(IEnumerable<T> model, bool force = false, CancellationToken cancellationToken = default)
        {
            if (model == null || !model.Any())
            {
                return;
            }
            if (force)
            {
                await Repository.CreateAsync(model, replaceIfExist: true, cancellationToken: cancellationToken);
            }
            else
            {
                IEnumerable<IGrouping<bool, T>> groups = model.GroupBy(m => m.ETag == null);

                foreach (IGrouping<bool, T> group in groups)
                {
                    if (group.Key)
                    {
                        await Repository.CreateAsync(group, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await Repository.UpdateAsync(group, cancellationToken: cancellationToken);
                    }
                }
            }

            if (Initialized)
            {
                T c = model.SingleOrDefault(m => m.PartitionKey == PartitionKey && m.RowKey == rowKey);
                if (c != null)
                {
                    cache = c;
                }
            }
        }
    }
}