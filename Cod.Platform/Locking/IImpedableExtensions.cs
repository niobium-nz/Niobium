using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Cod.Platform.Locking
{
    public static class IImpedableExtensions
    {
        public static async Task ImpedeAsync(this IImpedable impedable, string category, int cause, string policyInput = null)
        {
            await impedable.ImpedeAsync(category, new int[] { cause }, policyInput);
        }

        public static async Task ImpedeAsync(this IImpedable impedable, string category, IEnumerable<int> causes, string policyInput = null)
        {
            foreach (int cause in causes)
            {
                IImpedimentContext context = new()
                {
                    ImpedementID = impedable.GetImpedementID(),
                    Category = category,
                    Cause = cause,
                    PolicyInput = policyInput
                };

                foreach (IImpedimentPolicy policy in impedable.ImpedimentPolicies)
                {
                    await policy.ImpedeAsync(context);
                }
            }
        }

        public static async Task UnimpedeAsync(this IImpedable impedable, string category, int cause, string policyInput = null)
        {
            await impedable.UnimpedeAsync(category, new int[] { cause }, policyInput);
        }

        public static async Task UnimpedeAsync(this IImpedable impedable, string category, IEnumerable<int> causes, string policyInput = null)
        {
            await impedable.UnimpedeAsync(impedable.GetImpedementID(), category, causes, policyInput);
        }

        public static async Task UnimpedeAsync(this IImpedable impedable, string impedementID, string category, IEnumerable<int> causes, string policyInput = null)
        {
            foreach (int cause in causes)
            {
                IImpedimentContext context = new()
                {
                    ImpedementID = impedementID,
                    Category = category,
                    Cause = cause,
                    PolicyInput = policyInput,
                };

                foreach (IImpedimentPolicy policy in impedable.ImpedimentPolicies)
                {
                    await policy.UnimpedeAsync(context);
                }
            }
        }

        public static IAsyncEnumerable<Impediment> GetImpedimentsByCategoryAsync(this IImpedable impedable, string category, CancellationToken cancellationToken = default)
        {
            return string.IsNullOrEmpty(category)
                ? throw new ArgumentNullException(nameof(category))
                : impedable.GetImpedimentsAsync(category, cancellationToken: cancellationToken);
        }

        public static async Task<Impediment> GetImpedimentAsync(this IImpedable impedable, string category, int cause, CancellationToken cancellationToken = default)
        {
            if (category is null)
            {
                throw new ArgumentNullException(nameof(category));
            }
            Impediment[] impediments = await impedable.GetImpedimentsAsync(category, cause, cancellationToken: cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);
            return impediments.SingleOrDefault();
        }

        private static async IAsyncEnumerable<Impediment> GetImpedimentsAsync(this IImpedable impedable, string category = null, int cause = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ConcurrentDictionary<string, bool> existsLockers = new();
            foreach (IImpedimentPolicy policy in impedable.ImpedimentPolicies)
            {
                IImpedimentContext context = new()
                {
                    Category = category,
                    Cause = cause,
                    ImpedementID = impedable.GetImpedementID(),
                };

                if (await policy.SupportAsync(context, cancellationToken))
                {
                    IAsyncEnumerable<Impediment> impediments = policy.GetImpedimentsAsync(context, cancellationToken);
                    await foreach (Impediment item in impediments)
                    {
                        bool exist = existsLockers.AddOrUpdate($"{item.PartitionKey}|{item.RowKey}", false, (k, ev) => true);
                        if (!exist)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }
    }
}
