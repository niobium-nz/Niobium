using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    internal class OpenIDManager : IOpenIDManager
    {
        private readonly Lazy<IQueryableRepository<Model.OpenID>> repository;

        public OpenIDManager(Lazy<IQueryableRepository<Model.OpenID>> repository)
        {
            this.repository = repository;
        }

        public async Task<Model.OpenID> GetChannelAsync(string account, int kind, string identifier)
            => await this.repository.Value.GetAsync(
                OpenID.BuildPartitionKey(account),
                OpenID.BuildRowKey(kind, identifier));

        public async Task<IEnumerable<Model.OpenID>> GetChannelsAsync(string account)
            => await this.repository.Value.GetAsync(OpenID.BuildPartitionKey(account));

        public async Task<IEnumerable<Model.OpenID>> GetChannelsAsync(string account, int kind)
            => await this.repository.Value.GetAsync(
                OpenID.BuildPartitionKey(account),
                OpenID.BuildRowKeyStart(kind),
                OpenID.BuildRowKeyEnd(kind));

        public async Task RegisterAsync(IEnumerable<OpenIDRegistration> registrations)
        {
            // TODO (5he11) poor performance, better to use batch operation
            foreach (var registration in registrations)
            {
                var entity = new Model.OpenID
                {
                    PartitionKey = OpenID.BuildPartitionKey(registration.Account),
                    RowKey = OpenID.BuildRowKey(registration.Kind, registration.App, registration.OffsetPrefix),
                    Identity = registration.Identity,
                };
                await this.RetryRegistration(entity, registration.App, 0, registration.OverrideIfExists, registration.OffsetPrefix);
            }
        }

        private async Task RetryRegistration(Model.OpenID entity, string app, int retryCount, bool overrideIfExists, string offsetPrefix)
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

            try
            {
                await this.repository.Value.CreateAsync(entity, overrideIfExists);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 419)
                {
                    await this.RetryRegistration(entity, app, ++retryCount, overrideIfExists, offsetPrefix);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
