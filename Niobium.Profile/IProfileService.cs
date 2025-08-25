namespace Niobium.Profile
{
    public interface IProfileService<T> where T : class, IProfile
    {
        Task<T?> RetrieveAsync(bool forceRefresh = false, CancellationToken? cancellationToken = null);

        Task MergeAsync(T profile, CancellationToken? cancellationToken = null);
    }
}
