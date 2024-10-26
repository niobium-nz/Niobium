namespace Cod.File
{
    public interface IBlob
    {
        Task<OperationResult> UploadAsync(string container, string path, string contentType, Stream stream);
    }
}
