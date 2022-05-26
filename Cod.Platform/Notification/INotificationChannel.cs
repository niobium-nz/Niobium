namespace Cod.Platform
{
    public interface INotificationChannel
    {
        Task<OperationResult> SendAsync(
            string brand,
            Guid user,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0);
    }
}
