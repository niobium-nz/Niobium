using System.Runtime.CompilerServices;

namespace Cod.Platform
{
    public abstract class DefaultImpedimentPolicy : IImpedimentPolicy
    {
        private readonly Lazy<IRepository<Impediment>> repository;

        public DefaultImpedimentPolicy(Lazy<IRepository<Impediment>> repository) => this.repository = repository;

        public async IAsyncEnumerable<Impediment> GetImpedimentsAsync(IImpedimentContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (await this.SupportAsync(context, cancellationToken))
            {
                if (context.Cause != 0)
                {
                    var entity = await this.repository.Value.RetrieveAsync(
                        Impediment.BuildPartitionKey(context.ImpedementID, context.Category),
                        Impediment.BuildRowKey(context.Cause),
                        cancellationToken: cancellationToken);

                    if (entity != null)
                    {
                        yield return entity;
                    }
                }
                else
                {
                    var results = this.repository.Value.GetAsync(
                        Impediment.BuildPartitionKey(context.ImpedementID, context.Category),
                        cancellationToken: cancellationToken);
                    await foreach (var item in results)
                    {
                        yield return item;
                    }
                }
            }
        }

        public async Task<bool> ImpedeAsync(IImpedimentContext context, CancellationToken cancellationToken = default)
        {
            if (await this.SupportAsync(context, cancellationToken))
            {
                var pk = Impediment.BuildPartitionKey(context.ImpedementID, context.Category);
                var rk = Impediment.BuildRowKey(context.Cause);

                var existing = await this.repository.Value.RetrieveAsync(pk, rk, cancellationToken: cancellationToken);
                if (existing == null)
                {
                    await this.repository.Value.CreateAsync(new Impediment
                    {
                        PartitionKey = pk,
                        RowKey = rk,
                        Policy = context.PolicyInput,
                    }, 
                    replaceIfExist: true,
                    cancellationToken: cancellationToken);
                }
                else if (!existing.Policy.Contains(context.PolicyInput))
                {
                    existing.Policy += $",{context.PolicyInput}";
                    await this.repository.Value.UpdateAsync(existing, cancellationToken: cancellationToken);
                }

                return true;
            }
            return false;
        }

        public abstract Task<bool> SupportAsync(IImpedimentContext context, CancellationToken cancellationToken = default);

        public async Task<bool> UnimpedeAsync(IImpedimentContext context, CancellationToken cancellationToken = default)
        {
            if (await this.SupportAsync(context, cancellationToken))
            {
                var existing = await this.repository.Value.RetrieveAsync(
                    Impediment.BuildPartitionKey(context.ImpedementID, context.Category),
                    Impediment.BuildRowKey(context.Cause), 
                    cancellationToken: cancellationToken);
                if (existing != null)
                {
                    if (existing.Policy == context.PolicyInput)
                    {
                        await this.repository.Value.DeleteAsync(existing, preconditionCheck: false, successIfNotExist: true, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var policies = existing.Policy.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (policies.Contains(context.PolicyInput))
                        {
                            policies.Remove(context.PolicyInput);
                            if (policies.Count == 0)
                            {
                                await this.repository.Value.DeleteAsync(existing, preconditionCheck: false, successIfNotExist: true, cancellationToken: cancellationToken);
                            }
                            else
                            {
                                existing.Policy = String.Join(",", policies);
                                await this.repository.Value.UpdateAsync(existing, cancellationToken: cancellationToken);
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
