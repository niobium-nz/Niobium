namespace Cod.Profile
{
    public interface IProfileService<T> where T : IProfile
    {
        Task<T?> RetrieveAsync(CancellationToken cancellationToken);

        Task MergeAsync(T profile, CancellationToken cancellationToken);
    }
}
