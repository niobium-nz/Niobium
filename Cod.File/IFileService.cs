namespace Cod.File
{
    public interface IFileService
    {
        IAsyncEnumerable<string> GetPartitionsAsync(string? prefix = null, CancellationToken cancellationToken = default);

        IAsyncEnumerable<string> ListAsync(string partition, string? prefix = null, bool createIfNotExist = true, CancellationToken cancellationToken = default);

        Task<Stream?> GetAsync(string partition, string filename, bool createIfNotExist = true, CancellationToken cancellationToken = default);

        Task PutAsync(string partition, string filename, Stream stream, bool replaceIfExist = false, IDictionary<string, string>? tags = null, bool createIfNotExist = true, CancellationToken cancellationToken = default);

        Task TagAsync(string partition, string filename, IDictionary<string, string> tags, CancellationToken cancellationToken = default);

        Task DeleteAsync(string partition, IEnumerable<string> filename, bool ignoreIfNotExist = true, bool createIfNotExist = true, CancellationToken cancellationToken = default);
    }
}
