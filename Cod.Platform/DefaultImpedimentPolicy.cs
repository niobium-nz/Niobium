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
                var pk = Impediment.BuildPartitionKey(context.Entity.GetImpedementID(), context.Category);
                var rk = Impediment.BuildRowKey(context.Cause);

                var existing = await this.repository.Value.GetAsync(pk, rk);
                if (existing == null)
                {
                    await this.repository.Value.CreateAsync(new Impediment
                    {
                        PartitionKey = pk,
                        RowKey = rk,
                        Policy = context.PolicyInput,
                    }, true);
                }
                else if (!existing.Policy.Contains(context.PolicyInput))
                {
                    existing.Policy += $",{context.PolicyInput}";
                    await this.repository.Value.UpdateAsync(existing);
                }

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
                    var policies = existing.Policy.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (policies.Contains(context.PolicyInput))
                    {
                        policies.Remove(context.PolicyInput);
                        if (policies.Count == 0)
                        {
                            await this.repository.Value.DeleteAsync(existing);
                        }
                        else
                        {
                            existing.Policy = string.Join(",", policies);
                            await this.repository.Value.UpdateAsync(existing);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
