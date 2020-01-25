using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public abstract class PlatformDomain<T> : GenericDomain<T>, IPlatformDomain<T> where T : IEntity
    {
        private readonly Lazy<IRepository<T>> repository;
        private Func<Task<T>> getEntity;
        private string partitionKey;
        private string rowKey;
        private T cache;

        public override string PartitionKey => this.partitionKey;

        public override string RowKey => this.rowKey;

        protected IRepository<T> Repository => this.repository.Value;

        public PlatformDomain(Lazy<IRepository<T>> repository) => this.repository = repository;

        public async Task<T> GetEntityAsync()
        {
            if (this.cache == null)
            {
                this.cache = await this.getEntity();
            }
            return this.cache;
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

        protected async Task SaveEntityAsync()
            => await this.SaveEntityAsync(new[] { await this.GetEntityAsync() });

        protected async Task SaveEntityAsync(IEnumerable<T> model)
        {
            if (model == null)
            {
                return;
            }

            var groups = model.GroupBy(m => m.ETag == null);
            foreach (var group in groups)
            {
                if (group.Key)
                {
                    await this.Repository.CreateAsync(model);
                }
                else
                {
                    await this.Repository.UpdateAsync(model);
                }
            }
        }
    }
}
