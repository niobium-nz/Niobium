using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Contract;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public abstract class ImpedableDomain<T> : GenericDomain<T> where T : IEntity, IImpedable
    {
        private readonly Lazy<IRepository<T>> repository;
        private readonly Lazy<IEnumerable<IImpedimentPolicy>> policies;
        private readonly ILogger logger;

        public ImpedableDomain(Lazy<IRepository<T>> repository,
            Lazy<IEnumerable<IEventHandler<IDomain<T>>>> eventHandlers,
            Lazy<IEnumerable<IImpedimentPolicy>> policies,
            ILogger logger)
            : base(eventHandlers)
        {
            this.repository = repository;
            this.policies = policies;
            this.logger = logger;
        }

        public async Task ImpedeAsync(string category, int cause, string policyInput = null)
        {
            var context = new IImpedimentContext<T>
            {
                Entity = this.Entity,
                Category = category,
                Cause = cause,
                Logger = this.logger,
                PolicyInput = policyInput
            };

            foreach (var policy in this.policies.Value)
            {
                await policy.ImpedeAsync(context);
            }

            this.Entity.Impeded = true;
            await this.repository.Value.UpdateAsync(this.Entity);
        }

        public async Task UnimpedeAsync(string category, int cause, string policyInput = null)
        {
            var context = new IImpedimentContext<T>
            {
                Entity = this.Entity,
                Category = category,
                Cause = cause,
                PolicyInput = policyInput,
                Logger = this.logger
            };

            foreach (var policy in this.policies.Value)
            {
                await policy.UnimpedeAsync(context);
            }

            var existings = await this.GetImpedimentsAsync();
            if (existings.Count == 0)
            {
                this.Entity.Impeded = false;
                await this.repository.Value.UpdateAsync(this.Entity);
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
                    Entity = this.Entity,
                };

                existsLockers.AddRange(await policy.GetImpedimentsAsync(context));
            }
            return existsLockers;
        }
    }
}
