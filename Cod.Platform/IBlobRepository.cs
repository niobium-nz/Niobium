namespace Cod.Platform
{
    public interface IBlobRepository
    {
        Task CreateIfNotExists(string container);

        Task<IEnumerable<Uri>> ListAsync(string container, string prefix);

        Task DeleteAsync(IEnumerable<Uri> blobUris);

        Task PutAsync(string container, string blob, Stream stream, bool replaceIfExist);
    }
}
