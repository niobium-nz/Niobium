using Cod.Platform.Tenant;

namespace Cod.Platform.Identity
{
    internal class OpenIDManager : IOpenIDManager
    {
        private readonly Lazy<IQueryableRepository<OpenID>> repository;

        public OpenIDManager(Lazy<IQueryableRepository<OpenID>> repository)
        {
            this.repository = repository;
        }

        public IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, CancellationToken cancellationToken = default)
        {
            return repository.Value.GetAsync(OpenID.BuildPartitionKey(user), cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, int kind, CancellationToken cancellationToken = default)
        {
            return repository.Value.QueryAsync(OpenID.BuildPartitionKey(user), OpenID.BuildRowKeyStart(kind), OpenID.BuildRowKeyEnd(kind), cancellationToken: cancellationToken);
        }

        public async Task RegisterAsync(IEnumerable<OpenIDRegistration> registrations, CancellationToken cancellationToken = default)
        {
            // TODO (5he11) poor performance, better to use batch operation
            foreach (OpenIDRegistration registration in registrations)
            {
                if (registration.Kind == (int)OpenIDKind.Username)
                {
                    continue;
                }

                OpenID entity = new()
                {
                    PartitionKey = OpenID.BuildPartitionKey(registration.User),
                    RowKey = OpenID.BuildRowKey(registration.Kind, registration.App, registration.OffsetPrefix),
                    Identity = registration.Identity,
                };
                await RetryRegistration(entity, registration.App, 0, registration.ForceOffset0, registration.OffsetPrefix, cancellationToken);
            }
        }

        private async Task RetryRegistration(OpenID entity, string app, int retryCount, bool forceOffset0, string offsetPrefix, CancellationToken cancellationToken)
        {
            int kind = entity.GetKind();
            entity.RowKey = string.IsNullOrWhiteSpace(app)
                ? retryCount == 0
                    ? OpenID.BuildRowKey(kind, offsetPrefix)
                    : OpenID.BuildRowKey(kind, $"{offsetPrefix}{retryCount}")
                : retryCount == 0
                    ? OpenID.BuildRowKey(kind, app, offsetPrefix)
                    : OpenID.BuildRowKey(kind, app, $"{offsetPrefix}{retryCount}");

            if (forceOffset0)
            {
                await repository.Value.CreateAsync(entity, replaceIfExist: true, cancellationToken: cancellationToken);
            }
            else
            {
                OpenID existing = await repository.Value.RetrieveAsync(entity.PartitionKey, entity.RowKey, cancellationToken: cancellationToken);
                if (existing == null)
                {
                    await repository.Value.CreateAsync(entity, replaceIfExist: true, cancellationToken: cancellationToken);
                }
                else
                {
                    // REMARK (5he11) 如果既有的数据的值一模一样则什么都不干，如果不一样则offset+1
                    if (existing.Identity == entity.Identity)
                    {
                        return;
                    }
                    else
                    {
                        await RetryRegistration(entity, app, ++retryCount, forceOffset0, offsetPrefix, cancellationToken);
                    }
                }
            }
        }
    }
}
