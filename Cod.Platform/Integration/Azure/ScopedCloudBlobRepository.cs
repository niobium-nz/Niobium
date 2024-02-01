namespace Cod.Platform.Integration.Azure
{
    public abstract class ScopedCloudBlobRepository
    {
        protected IBlobRepository Repository { get; private set; }

        protected virtual bool CreateIfNotExist => true;

        protected abstract string ContainerName { get; }

        public ScopedCloudBlobRepository(IBlobRepository repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public IAsyncEnumerable<string> ListAsync(string prefix, CancellationToken cancellationToken = default)
        {
            return Repository.ListAsync(ContainerName, prefix, createIfNotExist: CreateIfNotExist, cancellationToken: cancellationToken);
        }

        public async Task GetAsync(string blobName, Stream destination, CancellationToken cancellationToken = default)
        {
            await Repository.GetAsync(ContainerName, blobName, destination, createIfNotExist: CreateIfNotExist, cancellationToken: cancellationToken);
        }

        public async Task PutAsync(string blobName, Stream stream, bool replaceIfExist = false, IDictionary<string, string> tags = null, CancellationToken cancellationToken = default)
        {
            await Repository.PutAsync(ContainerName, blobName, stream, replaceIfExist: replaceIfExist, tags: tags, createIfNotExist: CreateIfNotExist, cancellationToken: cancellationToken);
        }

        public async Task DeleteAsync(IEnumerable<string> blobNames, bool ignoreIfNotExist = true, CancellationToken cancellationToken = default)
        {
            await Repository.DeleteAsync(ContainerName, blobNames, ignoreIfNotExist: ignoreIfNotExist, createIfNotExist: CreateIfNotExist, cancellationToken: cancellationToken);
        }
    }
}
