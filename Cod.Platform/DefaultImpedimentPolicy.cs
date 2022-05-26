using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public abstract class DefaultImpedimentPolicy : IImpedimentPolicy
    {
        private readonly Lazy<IRepository<Impediment>> repository;

        public DefaultImpedimentPolicy(Lazy<IRepository<Impediment>> repository) => this.repository = repository;

        public async Task<IEnumerable<Impediment>> GetImpedimentsAsync(IImpedimentContext context)
        {
            if (await this.SupportAsync(context))
            {
                var table = CloudStorage.GetTable<Impediment>();
                var filter = TableQuery.GenerateFilterCondition(nameof(Impediment.PartitionKey),
                    QueryComparisons.Equal, Impediment.BuildPartitionKey(context.ImpedementID, context.Category));

                if (context.Cause != 0)
                {
                    filter = TableQuery.CombineFilters(filter, TableOperators.And, TableQuery.GenerateFilterCondition(nameof(Impediment.RowKey),
                    QueryComparisons.Equal, Impediment.BuildRowKey(context.Cause)));
                }

                return await table.WhereAsync<Impediment>(filter);
            }
            return Enumerable.Empty<Impediment>();
        }

        public async Task<bool> ImpedeAsync(IImpedimentContext context)
        {
            if (await this.SupportAsync(context))
            {
                var pk = Impediment.BuildPartitionKey(context.ImpedementID, context.Category);
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

        public abstract Task<bool> SupportAsync(IImpedimentContext context);

        public async Task<bool> UnimpedeAsync(IImpedimentContext context)
        {
            if (await this.SupportAsync(context))
            {
                var existing = await this.repository.Value.GetAsync(Impediment.BuildPartitionKey(context.ImpedementID, context.Category), Impediment.BuildRowKey(context.Cause));
                if (existing != null)
                {
                    if (existing.Policy == context.PolicyInput)
                    {
                        await this.repository.Value.DeleteAsync(existing);
                    }
                    else
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
                                existing.Policy = String.Join(",", policies);
                                await this.repository.Value.UpdateAsync(existing);
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
