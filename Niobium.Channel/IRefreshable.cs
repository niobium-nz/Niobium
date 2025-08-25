namespace Niobium.Channel
{
    public interface IRefreshable
    {
        Task RefreshAsync(CancellationToken cancellationToken = default);
    }
}
