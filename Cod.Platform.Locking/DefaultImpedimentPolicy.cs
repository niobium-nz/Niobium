using System.Runtime.CompilerServices;

namespace Cod.Platform.Locking
{
    public abstract class DefaultImpedimentPolicy(Lazy<IRepository<Impediment>> repository) : IImpedimentPolicy
    {
        public async IAsyncEnumerable<Impediment> GetImpedimentsAsync(IImpedimentContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.Category))
            {
                throw new ArgumentNullException(nameof(context), "Context and category must not be null or empty.");
            }

            if (await SupportAsync(context, cancellationToken))
            {
                if (context.Cause != 0)
                {
                    var entity = await repository.Value.RetrieveAsync(
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
                    IAsyncEnumerable<Impediment> results = repository.Value.GetAsync(
                        Impediment.BuildPartitionKey(context.ImpedementID, context.Category),
                        cancellationToken: cancellationToken);
                    await foreach (Impediment item in results)
                    {
                        yield return item;
                    }
                }
            }
        }

        public async Task<bool> ImpedeAsync(IImpedimentContext context, CancellationToken cancellationToken = default)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.Category))
            {
                throw new ArgumentNullException(nameof(context), "Context and category must not be null or empty.");
            }

            if (await SupportAsync(context, cancellationToken))
            {
                string pk = Impediment.BuildPartitionKey(context.ImpedementID, context.Category);
                string rk = Impediment.BuildRowKey(context.Cause);

                var existing = await repository.Value.RetrieveAsync(pk, rk, cancellationToken: cancellationToken);
                if (existing == null)
                {
                    await repository.Value.CreateAsync(new Impediment
                    {
                        PartitionKey = pk,
                        RowKey = rk,
                        Policy = context.PolicyInput,
                    },
                    replaceIfExist: true,
                    cancellationToken: cancellationToken);
                }
                else if (existing.Policy != null && context.PolicyInput != null && !existing.Policy.Contains(context.PolicyInput))
                {
                    existing.Policy += $",{context.PolicyInput}";
                    await repository.Value.UpdateAsync(existing, cancellationToken: cancellationToken);
                }

                return true;
            }
            return false;
        }

        public abstract Task<bool> SupportAsync(IImpedimentContext context, CancellationToken cancellationToken = default);

        public async Task<bool> UnimpedeAsync(IImpedimentContext context, CancellationToken cancellationToken = default)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.Category))
            {
                throw new ArgumentNullException(nameof(context), "Context and category must not be null or empty.");
            }

            if (await SupportAsync(context, cancellationToken))
            {
                var existing = await repository.Value.RetrieveAsync(
                    Impediment.BuildPartitionKey(context.ImpedementID, context.Category),
                    Impediment.BuildRowKey(context.Cause),
                    cancellationToken: cancellationToken);
                if (existing != null)
                {
                    if (existing.Policy == context.PolicyInput)
                    {
                        await repository.Value.DeleteAsync(existing, preconditionCheck: false, successIfNotExist: true, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        List<string> policies = existing.Policy?.Split([","], StringSplitOptions.RemoveEmptyEntries).ToList() ?? [];
                        if (context.PolicyInput != null && policies.Contains(context.PolicyInput))
                        {
                            policies.Remove(context.PolicyInput);
                            if (policies.Count == 0)
                            {
                                await repository.Value.DeleteAsync(existing, preconditionCheck: false, successIfNotExist: true, cancellationToken: cancellationToken);
                            }
                            else
                            {
                                existing.Policy = string.Join(",", policies);
                                await repository.Value.UpdateAsync(existing, cancellationToken: cancellationToken);
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
