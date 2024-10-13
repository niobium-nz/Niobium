namespace Cod.Storage.Blob
{
    public interface IBlob
    {
        Task<OperationResult> UploadAsync(string container, string path, string contentType, Stream stream);
    }
}
