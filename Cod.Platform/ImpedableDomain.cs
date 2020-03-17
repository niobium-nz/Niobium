using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Cod.Platform
{
    public abstract class ImpedableDomain<T> : PlatformDomain<T> where T : IEntity, IImpedable
    {
        private readonly Lazy<IEnumerable<IImpedimentPolicy>> policies;
        private readonly ILogger logger;

        public ImpedableDomain(Lazy<IRepository<T>> repository,
            Lazy<IEnumerable<IImpedimentPolicy>> policies,
            ILogger logger)
            : base(repository)
        {
            this.policies = policies;
            this.logger = logger;
        }

        public async Task ImpedeAsync(string category, int cause, string policyInput = null)
        {
            var entity = await this.GetEntityAsync();
            var context = new IImpedimentContext<T>
            {
                Entity = entity,
                Category = category,
                Cause = cause,
                Logger = this.logger,
                PolicyInput = policyInput
            };

            foreach (var policy in this.policies.Value)
            {
                await policy.ImpedeAsync(context);
            }

            entity.Impeded = true;
            await this.Repository.UpdateAsync(entity);
        }

        public async Task UnimpedeAsync(string category, int cause, string policyInput = null)
        {
            var entity = await this.GetEntityAsync();
            var context = new IImpedimentContext<T>
            {
                Entity = entity,
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
                entity.Impeded = false;
                await this.Repository.UpdateAsync(entity);
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

        public async Task<Impediment> GetImpedimentAsync(string category, int cause)
        {
            if (category is null)
            {
                throw new ArgumentNullException(nameof(category));
            }
            var impediments = await this.GetImpedimentsAsync(category, cause);
            return impediments.SingleOrDefault();
        }

        private async Task<IReadOnlyList<Impediment>> GetImpedimentsAsync(string category = null, int cause = 0)
        {
            var existsLockers = new List<Impediment>();
            foreach (var policy in this.policies.Value)
            {
                var context = new IImpedimentContext<T>()
                {
                    Category = category,
                    Cause = cause,
                    Entity = await this.GetEntityAsync(),
                };

                existsLockers.AddRange(await policy.GetImpedimentsAsync(context));
            }
            return existsLockers;
        }
    }
}
