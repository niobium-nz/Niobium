using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    internal class ImpedementPolicyScanProvider : IImpedimentPolicy
    {
        public async Task<IEnumerable<Impediment>> GetImpedimentsAsync(IImpedimentContext context)
        {
            var table = CloudStorage.GetTable<Impediment>();
            var filter = TableQuery.CombineFilters(TableQuery.GenerateFilterCondition(nameof(Impediment.PartitionKey),
                    QueryComparisons.GreaterThan, Impediment.BuildPartitionKey(context.ImpedementID, "0")),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(Impediment.PartitionKey),
                    QueryComparisons.LessThan, Impediment.BuildPartitionKey(context.ImpedementID, "Z")));
            return await table.WhereAsync<Impediment>(filter);
        }

        public Task<bool> ImpedeAsync(IImpedimentContext context) => Task.FromResult(false);

        public Task<bool> SupportAsync(IImpedimentContext context) => Task.FromResult(context != null && String.IsNullOrEmpty(context.Category));

        public Task<bool> UnimpedeAsync(IImpedimentContext context) => Task.FromResult(false);
    }
}
