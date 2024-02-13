namespace Cod.Platform
{
    public interface IImpedimentPolicy
    {
        Task<bool> SupportAsync(IImpedimentContext context, CancellationToken cancellationToken = default);

        Task<bool> ImpedeAsync(IImpedimentContext context, CancellationToken cancellationToken = default);

        Task<bool> UnimpedeAsync(IImpedimentContext context, CancellationToken cancellationToken = default);

        IAsyncEnumerable<Impediment> GetImpedimentsAsync(IImpedimentContext context, CancellationToken cancellationToken = default);
    }
}
