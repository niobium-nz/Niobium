namespace Cod.Platform
{
    internal class ImpedementPolicyScanProvider : IImpedimentPolicy
    {
        private readonly Lazy<IQueryableRepository<Impediment>> repo;

        public ImpedementPolicyScanProvider(Lazy<IQueryableRepository<Impediment>> repo)
        {
            this.repo = repo;
        }

        public IAsyncEnumerable<Impediment> GetImpedimentsAsync(IImpedimentContext context, CancellationToken cancellationToken = default)
        {
            return this.repo.Value.QueryAsync(
                Impediment.BuildPartitionKey(context.ImpedementID, "0"),
                Impediment.BuildPartitionKey(context.ImpedementID, "Z"),
                cancellationToken: cancellationToken);
        }

        public Task<bool> ImpedeAsync(IImpedimentContext context, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> SupportAsync(IImpedimentContext context, CancellationToken cancellationToken = default) => Task.FromResult(context != null && String.IsNullOrEmpty(context.Category));

        public Task<bool> UnimpedeAsync(IImpedimentContext context, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
