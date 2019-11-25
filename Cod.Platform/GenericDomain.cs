using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public abstract class GenericDomain<T> : ILoggerSite, IDomain<T> where T : ITableEntity
    {
        protected Lazy<IRepository<T>> Pepository { get; private set; }
        private readonly Lazy<IEnumerable<IEventHandler<T>>> eventHandlers;
        private Func<Task<T>> getModel;
        private Func<Task<string>> getPartitionKey;
        private Func<Task<string>> getRowKey;

        private T cache;

        protected IRepository<T> Repository => this.Pepository.Value;

        public ILogger Logger { get; private set; }

        public bool Initialized { get; private set; }

        public GenericDomain(Lazy<IRepository<T>> repository,
            Lazy<IEnumerable<IEventHandler<T>>> eventHandlers,
            ILogger logger)
        {
            this.Pepository = repository;
            this.eventHandlers = eventHandlers;
            this.Logger = logger;
        }

        public virtual void Initialize() => this.Initialized = true;

        public void Initialize(T model)
        {
            if (!this.Initialized)
            {
                this.getModel = () => Task.FromResult(model);
                this.getPartitionKey = () => Task.FromResult(model.PartitionKey);
                this.getRowKey = () => Task.FromResult(model.RowKey);
            }
            this.Initialize();
        }

        public void Initialize(string partitionKey, string rowkey)
        {
            if (!this.Initialized)
            {
                this.getModel = async () => await this.GetModelAsync(partitionKey, rowkey);
                this.getPartitionKey = () => Task.FromResult(partitionKey);
                this.getRowKey = () => Task.FromResult(rowkey);
            }
            this.Initialize();
        }

        protected async Task TriggerAsync(object e)
        {
            foreach (var eventHandler in this.eventHandlers.Value)
            {
                await eventHandler.HandleAsync(this, e);
            }
        }

        protected async Task<string> GetRowKeyAsync() => await this.getRowKey();

        protected async Task<string> GetPartitionKeyAsync() => await this.getPartitionKey();

        protected virtual void OnInitialized(T model) { }

        protected virtual async Task SubmitChangesAsync()
            => await this.Repository.UpdateAsync(new[] { await this.GetModelAsync() });

        public async Task<T> GetModelAsync()
        {
            if (this.cache == null)
            {
                this.cache = await this.getModel();
                if (this.cache != null)
                {
                    this.OnInitialized(this.cache);
                }
            }
            return this.cache;
        }

        public async Task SaveModelAsync()
            => await this.SaveModelAsync(new[] { await this.GetModelAsync() });

        protected async Task<T> GetModelAsync(string partitionKey, string rowkey)
            => await this.Repository.GetAsync(partitionKey, rowkey);

        protected async Task SaveModelAsync(IEnumerable<T> model)
            => await this.Repository.UpdateAsync(model);
    }
}
