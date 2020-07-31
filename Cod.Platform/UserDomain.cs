using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cod.Platform.Model;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class UserDomain : ImpedableDomain<Model.User>, IAccountable
    {
        private readonly Lazy<ICacheStore> cache;
        private readonly Lazy<IRepository<WechatEntity>> wechatRepository;
        private readonly Lazy<IRepository<Model.Login>> loginRepository;
        private readonly Lazy<IRepository<Model.Entitlement>> entitlementRepository;
        private readonly Lazy<IOpenIDManager> openIDManager;
        private readonly Lazy<ITokenManager> tokenManager;
        private readonly ILogger logger;

        public UserDomain(
            Lazy<ICacheStore> cache,
            Lazy<IRepository<Model.User>> repository,
            Lazy<IRepository<WechatEntity>> wechatRepository,
            Lazy<IRepository<Model.Login>> loginRepository,
            Lazy<IRepository<Model.Entitlement>> entitlementRepository,
            Lazy<IOpenIDManager> openIDManager,
            Lazy<IEnumerable<IImpedimentPolicy>> policies,
            Lazy<ITokenManager> tokenManager,
            ILogger logger)
            : base(repository, policies, logger)
        {
            this.cache = cache;
            this.wechatRepository = wechatRepository;
            this.loginRepository = loginRepository;
            this.entitlementRepository = entitlementRepository;
            this.openIDManager = openIDManager;
            this.tokenManager = tokenManager;
            this.logger = logger;
        }

        public ICacheStore CacheStore => this.cache.Value;

        public Task<string> GetAccountingPrincipalAsync() => Task.FromResult(this.RowKey);

        public override string GetImpedementID() => User.GetImpedementID(new StorageKey
        {
            PartitionKey = this.PartitionKey,
            RowKey = this.RowKey,
        });

        public async Task<OperationResult<User>> LoginAsync(string username, string password)
        {
            var login = await loginRepository.Value.GetAsync(
                Login.BuildPartitionKey(OpenIDKind.Username),
                Login.BuildRowKey(username));
            if (login == null)
            {
                return OperationResult<User>.Create(InternalError.NotFound, null);
            }

            if (login.Credentials != password)
            {
                return OperationResult<User>.Create(InternalError.AuthenticationRequired, null);
            }

            var userID = login.User;
            var user = await this.Repository.GetAsync(
                User.BuildPartitionKey(userID),
                User.BuildRowKey(userID));
            if (user == null)
            {
                return OperationResult<User>.Create(InternalError.NotFound, null);
            }

            return OperationResult<User>.Create(user);
        }

        public async Task<OperationResult<User>> LoginAsync(OpenIDKind kind, string appID, string authCode)
        {
            if (kind != OpenIDKind.Wechat)
            {
                throw new NotSupportedException();
            }

            var wechat = await this.wechatRepository.Value.GetAsync(
                    WechatEntity.BuildOpenIDPartitionKey(appID),
                    WechatEntity.BuildOpenIDRowKey(authCode));
            if (wechat == null || String.IsNullOrWhiteSpace(wechat.Value))
            {
                return OperationResult<User>.Create(InternalError.AuthenticationRequired, null);
            }

            var openid = wechat.Value;
            var login = await loginRepository.Value.GetAsync(
                Login.BuildPartitionKey(kind, appID),
                Login.BuildRowKey(openid));
            if (login == null)
            {
                return OperationResult<User>.Create(InternalError.NotFound, null);
            }

            var userID = login.User;
            var user = await this.Repository.GetAsync(
                User.BuildPartitionKey(userID),
                User.BuildRowKey(userID));
            if (user == null)
            {
                return OperationResult<User>.Create(InternalError.NotFound, null);
            }

            if (user.Disabled)
            {
                return OperationResult<User>.Create(InternalError.Locked, null);
            }

            return OperationResult<User>.Create(user);
        }

        public async Task<string> IssueTokenAsync()
        {
            var entity = await this.GetEntityAsync();
            var userID = entity.GetID();
            var records = await this.entitlementRepository.Value.GetAsync(Entitlement.BuildPartitionKey(userID));
            var entitlements = records.Select(r => new KeyValuePair<string, string>(r.RowKey, r.Value));

            var openIDs = await this.openIDManager.Value.GetChannelsAsync(userID);
            var mobile = openIDs.SingleOrDefault(o => o.GetKind() == (int)OpenIDKind.SMS);
            if (mobile == null)
            {
                this.logger.LogWarning($"User {userID} does not have mobile open ID.");
            }

            var wechat = openIDs.SingleOrDefault(o => o.GetKind() == (int)OpenIDKind.Wechat);
            if (wechat == null)
            {
                this.logger.LogWarning($"User {userID} does not have Wechat open ID.");
            }

            return await this.tokenManager.Value.CreateAsync(
                userID,
                wechat?.Identity,
                mobile?.Identity,
                ((int)OpenIDProvider.Wechat).ToString(),
                wechat?.GetApp(),
                entity.Roles.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
                entitlements);
        }

        public async Task<OperationResult<Guid>> GetOrRegisterAsync(string mobile, string ip = null)
        {
            var login = await loginRepository.Value.GetAsync(
                    Login.BuildPartitionKey(OpenIDKind.SMS),
                    Login.BuildRowKey(mobile));
            if (login != null)
            {
                return OperationResult<Guid>.Create(login.User);
            }

            var registations = OpenIDRegistration.Build(mobile);
            var newUser = await this.RegisterAsync(registations, ip);
            if (!newUser.IsSuccess)
            {
                return new OperationResult<Guid>(newUser);
            }

            return OperationResult<Guid>.Create(newUser.Result.GetID());
        }

        public async Task<OperationResult<User>> RegisterAsync(IEnumerable<OpenIDRegistration> registrations, string ip)
        {
            Guid? user = null;
            foreach (var registration in registrations)
            {
                var login = await loginRepository.Value.GetAsync(
                    Login.BuildPartitionKey(registration.Kind, registration.App),
                    Login.BuildRowKey(registration.Identity));
                if (login != null)
                {
                    if (user.HasValue && login.User != user.Value)
                    {
                        return OperationResult<User>.Create(InternalError.Conflict, null);
                    }

                    user = login.User;
                }
            }

            if (!user.HasValue)
            {
                user = Guid.NewGuid();
            }

            foreach (var registration in registrations)
            {
                registration.User = user.Value;
                registration.OverrideIfExists = true;
            }

            await this.openIDManager.Value.RegisterAsync(registrations);

            var logins = new List<Model.Login>();
            foreach (var registration in registrations)
            {
                logins.Add(new Model.Login
                {
                    PartitionKey = Login.BuildPartitionKey(registration.Kind, registration.App),
                    RowKey = Login.BuildRowKey(registration.Identity),
                    User = user.Value,
                });
            }
            await this.loginRepository.Value.CreateAsync(logins, true);

            var newuser = new Model.User
            {
                PartitionKey = User.BuildPartitionKey(user.Value),
                RowKey = User.BuildRowKey(user.Value),
                Disabled = false,
                FirstIP = ip,
                LastIP = ip,
                Roles = Role.Customer,
            };

            await this.Repository.CreateAsync(newuser, true);
            var result = await this.Repository.GetAsync(
                User.BuildPartitionKey(user.Value),
                User.BuildRowKey(user.Value));
            return OperationResult<User>.Create(result);
        }

        public async Task<OperationResult<User>> ApplyAsync(string role, IReadOnlyDictionary<string, object> data)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentNullException(nameof(role));
            }

            role = role.ToUpperInvariant();

            var user = await this.GetEntityAsync();
            if (user == null)
            {
                return OperationResult<User>.Create(InternalError.NotFound, null);
            }

            var result = await this.OnApplyAsync(role, data, user);
            if (!result.IsSuccess)
            {
                return new OperationResult<User>(result);
            }

            if (String.IsNullOrEmpty(user.Roles))
            {
                user.Roles = $"{role}";
                await this.SaveEntityAsync();
            }
            else
            {
                var roles = user.Roles.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (!roles.Contains(role))
                {
                    user.Roles += $",{role}";
                    await this.SaveEntityAsync();
                }
            }

            return OperationResult<User>.Create(user);
        }

        protected virtual Task<OperationResult> OnApplyAsync(string role, IReadOnlyDictionary<string, object> data, Model.User user)
            => Task.FromResult(OperationResult.Create());
    }
}
