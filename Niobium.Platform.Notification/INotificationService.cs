namespace Niobium.Platform.Notification
{
    public interface INotificationService
    {
        Task<OperationResult<int>> SendAsync(
            string brand,
            Guid user,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters,
            int startLevel = 0,
            int maxLevel = 100);
    }
}
