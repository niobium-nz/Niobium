namespace Cod.Platform
{
    internal class OpenIDManager : IOpenIDManager
    {
        private readonly Lazy<IQueryableRepository<OpenID>> repository;

        public OpenIDManager(Lazy<IQueryableRepository<OpenID>> repository) => this.repository = repository;

        public IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, CancellationToken cancellationToken = default)
            => this.repository.Value.GetAsync(OpenID.BuildPartitionKey(user), cancellationToken: cancellationToken);

        public IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, int kind, CancellationToken cancellationToken = default)
            => this.repository.Value.QueryAsync(OpenID.BuildPartitionKey(user), OpenID.BuildRowKeyStart(kind), OpenID.BuildRowKeyEnd(kind), cancellationToken: cancellationToken);

        public async Task RegisterAsync(IEnumerable<OpenIDRegistration> registrations, CancellationToken cancellationToken = default)
        {
            // TODO (5he11) poor performance, better to use batch operation
            foreach (var registration in registrations)
            {
                if (registration.Kind == (int)OpenIDKind.Username)
                {
                    continue;
                }

                var entity = new OpenID
                {
                    PartitionKey = OpenID.BuildPartitionKey(registration.User),
                    RowKey = OpenID.BuildRowKey(registration.Kind, registration.App, registration.OffsetPrefix),
                    Identity = registration.Identity,
                };
                await this.RetryRegistration(entity, registration.App, 0, registration.ForceOffset0, registration.OffsetPrefix, cancellationToken);
            }
        }

        private async Task RetryRegistration(OpenID entity, string app, int retryCount, bool forceOffset0, string offsetPrefix, CancellationToken cancellationToken)
        {
            var kind = entity.GetKind();
            if (String.IsNullOrWhiteSpace(app))
            {
                if (retryCount == 0)
                {
                    entity.RowKey = OpenID.BuildRowKey(kind, offsetPrefix);
                }
                else
                {
                    entity.RowKey = OpenID.BuildRowKey(kind, $"{offsetPrefix}{retryCount}");
                }
            }
            else
            {
                if (retryCount == 0)
                {
                    entity.RowKey = OpenID.BuildRowKey(kind, app, offsetPrefix);
                }
                else
                {
                    entity.RowKey = OpenID.BuildRowKey(kind, app, $"{offsetPrefix}{retryCount}");
                }
            }

            if (forceOffset0)
            {
                await this.repository.Value.CreateAsync(entity, replaceIfExist: true, cancellationToken: cancellationToken);
            }
            else
            {
                var existing = await this.repository.Value.RetrieveAsync(entity.PartitionKey, entity.RowKey, cancellationToken: cancellationToken);
                if (existing == null)
                {
                    await this.repository.Value.CreateAsync(entity, replaceIfExist: true, cancellationToken: cancellationToken);
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
                        await this.RetryRegistration(entity, app, ++retryCount, forceOffset0, offsetPrefix, cancellationToken);
                    }
                }
            }
        }
    }
}
