using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Contract;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public abstract class ImpedableDomain<T> : GenericDomain<T> where T : ITableEntity, IImpedable
    {
        private readonly Lazy<IEnumerable<IImpedimentPolicy>> policies;

        public ImpedableDomain(Lazy<IRepository<T>> repository,
            Lazy<IEnumerable<IEventHandler<T>>> eventHandlers,
            Lazy<IEnumerable<IImpedimentPolicy>> policies,
            ILogger logger)
            : base(repository, eventHandlers, logger)
            => this.policies = policies;

        public async Task ImpedeAsync(string category, int cause, ILogger logger, string policyInput = null)
        {
            var model = await this.GetModelAsync();
            var context = new IImpedimentContext<T>
            {
                Entity = model,
                Category = category,
                Cause = cause,
                Log = logger,
                PolicyInput = policyInput
            };

            foreach (var policy in this.policies.Value)
            {
                await policy.ImpedeAsync(context, logger);
            }

            model.Impeded = true;
            await this.SaveModelAsync();
        }

        public async Task UnimpedeAsync(string category, int cause, ILogger logger, string policyInput = null)
        {
            var model = await this.GetModelAsync();
            var context = new IImpedimentContext<T>
            {
                Entity = model,
                Category = category,
                Cause = cause,
                PolicyInput = policyInput,
                Log = logger
            };

            foreach (var policy in this.policies.Value)
            {
                await policy.UnimpedeAsync(context);
            }

            var existings = await this.GetImpedimentsAsync();
            if (existings.Count == 0)
            {
                model.Impeded = false;
                await this.SaveModelAsync();
            }
        }

        public async Task<IReadOnlyList<Impediment>> GetImpedimentsByCategoryAsync(string category)
        {
            if (String.IsNullOrEmpty(category))
            {
                throw new ArgumentException(nameof(category));
            }
            return await this.GetImpedimentsAsync(category);
        }

        private async Task<IReadOnlyList<Impediment>> GetImpedimentsAsync(string category = null)
        {
            var existsLockers = new List<Impediment>();
            foreach (var policy in this.policies.Value)
            {
                var context = new IImpedimentContext<T>()
                {
                    Category = category,
                    Entity = await this.GetModelAsync()
                };

                existsLockers.AddRange(await policy.GetImpedimentsAsync(context));
            }
            return existsLockers;
        }
    }
}
