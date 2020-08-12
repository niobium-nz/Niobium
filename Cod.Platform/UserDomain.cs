using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cod.Platform.Model;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class UserDomain : ImpedableDomain<Model.User>, IAccountable, ILoggerSite
    {
        private readonly Lazy<ICacheStore> cache;
        private readonly Lazy<IRepository<WechatEntity>> wechatRepository;
        private readonly Lazy<IRepository<Model.Login>> loginRepository;
        private readonly Lazy<IRepository<Model.Entitlement>> entitlementRepository;
        private readonly Lazy<IOpenIDManager> openIDManager;
        private readonly Lazy<ITokenBuilder> tokenBuilder;

        public UserDomain(
            Lazy<ICacheStore> cache,
            Lazy<IRepository<Model.User>> repository,
            Lazy<IRepository<WechatEntity>> wechatRepository,
            Lazy<IRepository<Model.Login>> loginRepository,
            Lazy<IRepository<Model.Entitlement>> entitlementRepository,
            Lazy<IOpenIDManager> openIDManager,
            Lazy<IEnumerable<IImpedimentPolicy>> policies,
            Lazy<ITokenBuilder> tokenBuilder,
            ILogger logger)
            : base(repository, policies, logger)
        {
            this.cache = cache;
            this.wechatRepository = wechatRepository;
            this.loginRepository = loginRepository;
            this.entitlementRepository = entitlementRepository;
            this.openIDManager = openIDManager;
            this.tokenBuilder = tokenBuilder;
            this.Logger = logger;
        }

        public ICacheStore CacheStore => this.cache.Value;

        public ILogger Logger { get; private set; }

        public Task<string> GetAccountingPrincipalAsync() => Task.FromResult(this.RowKey);

        public override string GetImpedementID() => User.GetImpedementID(new StorageKey
        {
            PartitionKey = this.PartitionKey,
            RowKey = this.RowKey,
        });

        public async Task<OperationResult<Model.User>> LoginAsync(string username, string password)
        {
            var login = await loginRepository.Value.GetAsync(
                Login.BuildPartitionKey(OpenIDKind.Username),
                Login.BuildRowKey(username));
            if (login == null)
            {
                return OperationResult<Model.User>.Create(InternalError.NotFound, null);
            }

            if (login.Credentials != password)
            {
                return OperationResult<Model.User>.Create(InternalError.AuthenticationRequired, null);
            }

            var userID = login.User;
            var user = await this.Repository.GetAsync(
                User.BuildPartitionKey(userID),
                User.BuildRowKey(userID));
            if (user == null)
            {
                return OperationResult<Model.User>.Create(InternalError.NotFound, null);
            }

            return OperationResult<Model.User>.Create(user);
        }

        public async Task<OperationResult<LoginResult>> LoginAsync(OpenIDKind kind, string appID, string authCode)
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
                return OperationResult<LoginResult>.Create(InternalError.AuthenticationRequired, null);
            }

            var openid = wechat.Value;
            var login = await loginRepository.Value.GetAsync(
                Login.BuildPartitionKey(kind, appID),
                Login.BuildRowKey(openid));
            if (login == null)
            {
                return OperationResult<LoginResult>.Create(InternalError.NotFound, openid);
            }

            var userID = login.User;
            var user = await this.Repository.GetAsync(
                User.BuildPartitionKey(userID),
                User.BuildRowKey(userID));
            if (user == null)
            {
                return OperationResult<LoginResult>.Create(InternalError.NotFound, openid);
            }

            if (user.Disabled)
            {
                return OperationResult<LoginResult>.Create(InternalError.Locked, openid);
            }

            return OperationResult<LoginResult>.Create(new LoginResult
            {
                User = user,
                OpenID = openid,
            });
        }

        public async Task<string> IssueTokenAsync(IEnumerable<KeyValuePair<string, string>> entitlements)
        {
            var entity = await this.GetEntityAsync();
            var userID = entity.GetID();
            var records = await this.entitlementRepository.Value.GetAsync(Entitlement.BuildPartitionKey(userID));
            var es = records.Select(r => new KeyValuePair<string, string>(r.RowKey, r.Value)).ToList();
            es.AddRange(entitlements);
            return await this.tokenBuilder.Value.BuildAsync(
                userID.ToKey(),
                entity.Roles.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
                es);
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

        public async Task<OperationResult<Model.User>> RegisterAsync(IEnumerable<OpenIDRegistration> registrations, string ip)
        {
            var newUser = false;
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
                        return OperationResult<Model.User>.Create(InternalError.Conflict, null);
                    }

                    user = login.User;
                }
            }

            if (!user.HasValue)
            {
                user = Guid.NewGuid();
                newUser = true;
            }

            foreach (var registration in registrations)
            {
                registration.User = user.Value;
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

            if (newUser)
            {
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
            }

            var result = await this.Repository.GetAsync(
                User.BuildPartitionKey(user.Value),
                User.BuildRowKey(user.Value));
            return OperationResult<Model.User>.Create(result);
        }

        public async Task<IEnumerable<OpenID>> GetChannelsAsync(OpenIDKind kind, string app)
            => (await this.GetChannelsAsync(kind)).Where(c => c.GetApp() == app);

        public async Task<IEnumerable<OpenID>> GetChannelsAsync(OpenIDKind kind)
            => await this.openIDManager.Value.GetChannelsAsync(Guid.Parse(this.RowKey), (int)kind);

        public async Task<OperationResult<Model.User>> ApplyAsync(string role, object parameter)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentNullException(nameof(role));
            }

            role = role.ToUpperInvariant();

            var user = await this.GetEntityAsync();
            if (user == null)
            {
                return OperationResult<Model.User>.Create(InternalError.NotFound, null);
            }

            var result = await this.OnApplyAsync(user, role, parameter);
            if (!result.IsSuccess)
            {
                return new OperationResult<Model.User>(result);
            }

            var u = user.AddRole(role);
            if (u)
            {
                await this.SaveEntityAsync();
            }

            return OperationResult<Model.User>.Create(user);
        }

        protected virtual Task<OperationResult> OnApplyAsync(Model.User user, string role, object data)
            => Task.FromResult(OperationResult.Create());
    }
}
