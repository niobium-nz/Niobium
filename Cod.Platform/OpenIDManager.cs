using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    internal class OpenIDManager : IOpenIDManager
    {
        private readonly Lazy<IQueryableRepository<OpenID>> repository;

        public OpenIDManager(Lazy<IQueryableRepository<OpenID>> repository)
        {
            this.repository = repository;
        }

        public async Task<OpenID> GetChannelAsync(string account, int kind, string identifier)
            => await this.repository.Value.GetAsync(
                OpenID.BuildPartitionKey(account),
                OpenID.BuildRowKey(kind, identifier));

        public async Task<IEnumerable<OpenID>> GetChannelsAsync(string account)
            => await this.repository.Value.GetAsync(OpenID.BuildPartitionKey(account));

        public async Task<IEnumerable<OpenID>> GetChannelsAsync(string account, int kind)
            => await this.repository.Value.GetAsync(
                OpenID.BuildPartitionKey(account),
                OpenID.BuildRowKeyStart(kind),
                OpenID.BuildRowKeyEnd(kind));

        public async Task RegisterAsync(string account, int kind, string identity, bool overrideIfExists)
        {
            var entity = new OpenID
            {
                PartitionKey = OpenID.BuildPartitionKey(account),
                Identity = identity,
            };
            await this.RetryRegistration(entity, 0, overrideIfExists);
        }

        private async Task RetryRegistration(OpenID entity, int retryCount, bool overrideIfExists)
        {
            var kind = entity.GetKind();
            if (retryCount == 0)
            {
                entity.RowKey = OpenID.BuildRowKey(kind);
            }
            else
            {
                entity.RowKey = OpenID.BuildRowKey(kind, retryCount.ToString());
            }

            try
            {
                await this.repository.Value.CreateAsync(entity, overrideIfExists);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 419)
                {
                    await this.RetryRegistration(entity, ++retryCount, overrideIfExists);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
