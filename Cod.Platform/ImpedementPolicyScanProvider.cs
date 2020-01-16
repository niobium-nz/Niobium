using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cod.Platform
{
    class ImpedementPolicyScanProvider : IImpedimentPolicy
    {
        public async Task<IEnumerable<Impediment>> GetImpedimentsAsync<T>(IImpedimentContext<T> context) where T : IImpedable
        {
            var table = CloudStorage.GetTable<Impediment>();
            var filter = TableQuery.CombineFilters(TableQuery.GenerateFilterCondition(nameof(Impediment.PartitionKey),
                    QueryComparisons.GreaterThan, Impediment.BuildPartitionKey(context.Entity.GetImpedementID(), "0")),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(Impediment.PartitionKey),
                    QueryComparisons.LessThan, Impediment.BuildPartitionKey(context.Entity.GetImpedementID(), "Z")));
            return await table.WhereAsync<Impediment>(filter);
        }

        public Task<bool> ImpedeAsync<T>(IImpedimentContext<T> context) where T : IImpedable
        {
            return Task.FromResult(false);
        }

        public Task<bool> SupportAsync<T>(IImpedimentContext<T> context) where T : IImpedable
        {
            return Task.FromResult(context != null && string.IsNullOrEmpty(context.Category));
        }

        public Task<bool> UnimpedeAsync<T>(IImpedimentContext<T> context) where T : IImpedable
        {
            return Task.FromResult(false);
        }
    }
}
