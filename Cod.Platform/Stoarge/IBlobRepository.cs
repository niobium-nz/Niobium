namespace Cod.Platform.Stoarge
{
    public interface IBlobRepository
    {
        IAsyncEnumerable<string> GetContainersAsync(string prefix = null, CancellationToken cancellationToken = default);

        IAsyncEnumerable<string> ListAsync(string containerName, string prefix = null, bool createIfNotExist = true, CancellationToken cancellationToken = default);

        Task GetAsync(string containerName, string blobName, Stream destination, bool createIfNotExist = true, CancellationToken cancellationToken = default);

        Task PutAsync(string containerName, string blobName, Stream stream, bool replaceIfExist = false, IDictionary<string, string> tags = null, bool createIfNotExist = true, CancellationToken cancellationToken = default);

        Task TagAsync(string containerName, string blobName, IDictionary<string, string> tags, CancellationToken cancellationToken = default);

        Task DeleteAsync(string containerName, IEnumerable<string> blobNames, bool ignoreIfNotExist = true, bool createIfNotExist = true, CancellationToken cancellationToken = default);
    }
}
