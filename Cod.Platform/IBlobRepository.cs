namespace Cod.Platform
{
    public interface IBlobRepository
    {
        IBlobRepository Initialize(string containerName, bool createIfNotExist = true);

        IAsyncEnumerable<string> ListAsync(string prefix, CancellationToken cancellationToken = default);

        Task GetAsync(string blobName, Stream destination, CancellationToken cancellationToken = default);

        Task PutAsync(string blobName, Stream stream, bool replaceIfExist = false, IDictionary<string, string> tags = null, CancellationToken cancellationToken = default);

        Task DeleteAsync(IEnumerable<string> blobNames, bool ignoreIfNotExist = true, CancellationToken cancellationToken = default);
    }
}
