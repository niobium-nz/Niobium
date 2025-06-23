namespace Cod
{
    public abstract class GenericDomain<T> : IDomain<T> where T : class
    {
        private readonly Lazy<IRepository<T>> repository;
        private T cache;
        private Func<CancellationToken, Task<T>> getEntity;
        private string etag;

        public bool Initialized { get; protected set; }

        public string PartitionKey { get; private set; }

        public string RowKey { get; private set; }


        public async Task<string> GetHashAsync(CancellationToken cancellationToken = default)
        {
            if (etag == null)
            {
                var entity = await GetEntityAsync(cancellationToken);
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

        public async Task<T> GetEntityAsync(CancellationToken cancellationToken = default)
        {
            cache ??= await getEntity(cancellationToken);
            return cache;
        }

        public IDomain<T> Initialize(string partitionKey, string rowKey)
        {
            if (!Initialized)
            {
                getEntity = async (cancellationToken) => await Repository.RetrieveAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
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
                getEntity = _ => Task.FromResult(entity);
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
            List<Func<EntityChangedEvent<T>>> events = new();
            List<T> results = new();
            if (model == null || !model.Any())
            {
                return model;
            }
            if (force)
            {
                var createdOrReplaced = await Repository.CreateAsync(model, replaceIfExist: true, cancellationToken: cancellationToken);
                results.AddRange(createdOrReplaced);
                events.AddRange(createdOrReplaced.Select(m => new Func<EntityChangedEvent<T>>(() => new EntityChangedEvent<T>(EntityChangeType.Created | EntityChangeType.Updated, m))));
            }
            else
            {
                IEnumerable<IGrouping<bool, T>> groups = model.GroupBy(m => !EntityMappingHelper.TryGetField<string>(m, EntityKeyKind.ETag, out _));
                foreach (IGrouping<bool, T> group in groups)
                {
                    if (group.Key)
                    {
                        var created = await Repository.CreateAsync(group, cancellationToken: cancellationToken);
                        results.AddRange(created);
                        events.AddRange(created.Select(m => new Func<EntityChangedEvent<T>>(() => new EntityChangedEvent<T>(EntityChangeType.Created, m))));
                    }
                    else
                    {
                        var updated = await Repository.UpdateAsync(group, cancellationToken: cancellationToken);
                        results.AddRange(updated);
                        foreach (var u in updated)
                        {
                            events.Add(new Func<EntityChangedEvent<T>>(() =>
                            {
                                return new EntityChangedEvent<T>(EntityChangeType.Updated, u);
                            }));
                        }
                    }
                }
            }

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

            foreach (var changedEvent in events)
            {
                await OnEvent(changedEvent, cancellationToken);
            }

            return results;
        }

        protected async Task OnEvent<TEventArgs>(TEventArgs e, CancellationToken cancellationToken = default) where TEventArgs : class
            => await EventHandlers.InvokeAsync(e, cancellationToken);

        protected async Task OnEvent<TEventArgs>(Func<TEventArgs> getEventArgs, CancellationToken cancellationToken = default) where TEventArgs : class
            => await EventHandlers.InvokeAsync(getEventArgs, cancellationToken);
    }
}
