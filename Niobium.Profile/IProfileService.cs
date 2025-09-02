namespace Niobium.Profile
{
    public interface IProfileService<T> where T : class, IProfile
    {
        Task<T?> RetrieveAsync(Guid tenant, Guid user, bool forceRefresh = false, CancellationToken? cancellationToken = null);

        Task MergeAsync(Guid tenant, Guid user, T profile, CancellationToken? cancellationToken = null);
    }
}
