namespace Cod
{
    public abstract class GenericDomain<T> : IDomain<T>
    {
        private readonly Lazy<IRepository<T>> repository;
        private T cache;
        private Func<Task<T>> getEntity;

        public bool Initialized { get; protected set; }

        public string PartitionKey { get; private set; }

        public string RowKey { get; private set; }

        private string etag;
        public async Task<string> GetHashAsync()
        {
            if (etag == null)
            {
                var entity = await GetEntityAsync();
                etag = EntityMappingHelper.GetField<string>(entity, EntityKeyKind.ETag);
            }

            return etag;
        }

        protected IRepository<T> Repository => repository.Value;

        protected IEnumerable<IDomainEventHandler<IDomain<T>>> EventHandlers { get; }

        protected GenericDomain(Lazy<IRepository<T>> repository, IEnumerable<IDomainEventHandler<IDomain<T>>> eventHandlers)
        {
            this.repository = repository;
            EventHandlers = eventHandlers;
        }

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
                PartitionKey = partitionKey;
                RowKey = rowKey;
            }
            Initialized = true;
            return this;
        }

        public async Task ReloadAsync()
        {
            if (!Initialized)
            {
                throw new NotSupportedException();
            }
            cache = await Repository.RetrieveAsync(PartitionKey, RowKey);
        }

        public IDomain<T> Initialize(T entity)
        {
            if (!Initialized)
            {
                getEntity = () => Task.FromResult(entity);
                PartitionKey = EntityMappingHelper.GetField<string>(entity, EntityKeyKind.PartitionKey);
                RowKey = EntityMappingHelper.GetField<string>(entity, EntityKeyKind.RowKey);
            }
            Initialized = true;
            return this;
        }

        protected async Task SaveAsync(bool force = false, CancellationToken cancellationToken = default)
        {
            await SaveAsync(new[] { await GetEntityAsync() }, force, cancellationToken: cancellationToken);
        }

        protected async Task<IEnumerable<T>> SaveAsync(IEnumerable<T> model, bool force = false, CancellationToken cancellationToken = default)
        {
            List<T> entitiesCreated = new();
            List<T> entitiesUpdated = new();
            List<T> results = new();
            if (model == null || !model.Any())
            {
                return model;
            }
            if (force)
            {
                var created = await Repository.CreateAsync(model, replaceIfExist: true, cancellationToken: cancellationToken);
                entitiesCreated.AddRange(created);
            }
            else
            {
                IEnumerable<IGrouping<bool, T>> groups = model.GroupBy(m => !EntityMappingHelper.TryGetField<string>(m, EntityKeyKind.ETag, out _));
                foreach (IGrouping<bool, T> group in groups)
                {
                    if (group.Key)
                    {
                        var created = await Repository.CreateAsync(group, cancellationToken: cancellationToken);
                        entitiesCreated.AddRange(created);
                    }
                    else
                    {
                        var updated = await Repository.UpdateAsync(group, cancellationToken: cancellationToken);
                        entitiesUpdated.AddRange(updated);
                    }
                }
            }

            results.AddRange(entitiesCreated);
            results.AddRange(entitiesUpdated);

            if (Initialized)
            {
                T c = results.SingleOrDefault(m =>
                    EntityMappingHelper.GetField<string>(m, EntityKeyKind.PartitionKey) == PartitionKey
                        && EntityMappingHelper.GetField<string>(m, EntityKeyKind.RowKey) == RowKey);
                if (c != null)
                {
                    cache = c;
                }
            }

            foreach (var entity in entitiesCreated)
            {
                await OnEvent(new EntityCreatedEventArgs<T>(entity), cancellationToken);
            }

            return results;
        }

        protected async Task OnEvent<TEventArgs>(TEventArgs e, CancellationToken cancellationToken = default) where TEventArgs : class
            => await EventHandlers.InvokeAsync(e, cancellationToken);
    }
}
