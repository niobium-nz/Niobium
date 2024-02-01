namespace Cod.Platform
{
    public interface IQueueSignatureIssuer
    {
        Task<Uri> IssueAsync(string queueName, DateTimeOffset expires, QueuePermissions permissions, CancellationToken cancellationToken = default);
    }
}
