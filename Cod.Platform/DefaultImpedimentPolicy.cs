using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public abstract class DefaultImpedimentPolicy : IImpedimentPolicy
    {
        private readonly Lazy<IRepository<Impediment>> repository;

        public DefaultImpedimentPolicy(Lazy<IRepository<Impediment>> repository) => this.repository = repository;

        public async Task<IEnumerable<Impediment>> GetImpedimentsAsync<T>(IImpedimentContext<T> context) where T : IImpedable
        {
            if (await this.SupportAsync(context))
            {
                var table = CloudStorage.GetTable<Impediment>();
                var filter = TableQuery.GenerateFilterCondition(nameof(Impediment.PartitionKey),
                    QueryComparisons.Equal, Impediment.BuildPartitionKey(context.Entity.GetImpedementID(), context.Category));

                return await table.WhereAsync<Impediment>(filter);
            }
            return Enumerable.Empty<Impediment>();
        }

        public async Task<bool> ImpedeAsync<T>(IImpedimentContext<T> context) where T : IImpedable
        {
            if (await this.SupportAsync(context))
            {
                await this.repository.Value.CreateAsync(new Impediment
                {
                    PartitionKey = Impediment.BuildPartitionKey(context.Entity.GetImpedementID(), context.Category),
                    RowKey = Impediment.BuildRowKey(context.Cause),
                    Policy = context.PolicyInput,
                }, true);
                return true;
            }
            return false;
        }

        public abstract Task<bool> SupportAsync<T>(IImpedimentContext<T> context) where T : IImpedable;

        public async Task<bool> UnimpedeAsync<T>(IImpedimentContext<T> context) where T : IImpedable
        {
            if (await this.SupportAsync(context))
            {
                var existing = await this.repository.Value.GetAsync(Impediment.BuildPartitionKey(context.Entity.GetImpedementID(), context.Category), Impediment.BuildRowKey(context.Cause));
                if (existing != null)
                {
                    if (existing.Policy == context.PolicyInput)
                    {
                        await this.repository.Value.DeleteAsync(existing);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
