using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public abstract class ImpedableDomain<T> : PlatformDomain<T>, IImpedable where T : IEntity
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

        public abstract string GetImpedementID();

        public async Task ImpedeAsync(string category, int cause, string policyInput = null) => await this.ImpedeAsync(category, new int[] { cause }, policyInput);

        public async Task ImpedeAsync(string category, IEnumerable<int> causes, string policyInput = null)
        {
            foreach (var cause in causes)
            {
                var context = new IImpedimentContext
                {
                    ImpedementID = this.GetImpedementID(),
                    Category = category,
                    Cause = cause,
                    Logger = this.logger,
                    PolicyInput = policyInput
                };

                foreach (var policy in this.policies.Value)
                {
                    await policy.ImpedeAsync(context);
                }
            }
        }

        public async Task UnimpedeAsync(string category, int cause, string policyInput = null) => await this.UnimpedeAsync(category, new int[] { cause }, policyInput);

        public async Task UnimpedeAsync(string category, IEnumerable<int> causes, string policyInput = null) => await this.UnimpedeAsync(this.GetImpedementID(), category, causes, policyInput);

        public async Task UnimpedeAsync(string impedementID, string category, IEnumerable<int> causes, string policyInput = null)
        {
            foreach (var cause in causes)
            {
                var context = new IImpedimentContext
                {
                    ImpedementID = impedementID,
                    Category = category,
                    Cause = cause,
                    PolicyInput = policyInput,
                    Logger = this.logger
                };

                foreach (var policy in this.policies.Value)
                {
                    await policy.UnimpedeAsync(context);
                }
            }
        }

        public async Task<IReadOnlyList<Impediment>> GetImpedimentsByCategoryAsync(string category)
        {
            if (String.IsNullOrEmpty(category))
            {
                throw new ArgumentNullException(nameof(category));
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
                var context = new IImpedimentContext()
                {
                    Category = category,
                    Cause = cause,
                    ImpedementID = this.GetImpedementID(),
                };

                if (await policy.SupportAsync(context))
                {
                    var impediments = await policy.GetImpedimentsAsync(context);
                    foreach (var item in impediments)
                    {
                        if (!existsLockers.Any(e => e.RowKey == item.RowKey && e.PartitionKey == item.PartitionKey))
                        {
                            existsLockers.Add(item);
                        }
                    }
                }
            }
            return existsLockers;
        }
    }
}
