using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class UserDomain : ImpedableDomain<User>, IAccountable, ILoggerSite
    {
        private const string ClientIDSplitString = "###";
        public const OpenIDKind DefaultOpenIDKind = OpenIDKind.Username;

        private readonly Lazy<ICacheStore> cache;
        private readonly Lazy<IRepository<WechatEntity>> wechatRepository;
        private readonly Lazy<IRepository<Login>> loginRepository;
        private readonly Lazy<IRepository<Entitlement>> entitlementRepository;
        private readonly Lazy<IOpenIDManager> openIDManager;
        private readonly Lazy<ITokenBuilder> tokenBuilder;
        private readonly Lazy<IConfigurationProvider> configuration;

        public UserDomain(
            Lazy<ICacheStore> cache,
            Lazy<IRepository<User>> repository,
            Lazy<IRepository<WechatEntity>> wechatRepository,
            Lazy<IRepository<Login>> loginRepository,
            Lazy<IRepository<Entitlement>> entitlementRepository,
            Lazy<IOpenIDManager> openIDManager,
            Lazy<IEnumerable<IImpedimentPolicy>> policies,
            Lazy<ITokenBuilder> tokenBuilder,
            Lazy<IConfigurationProvider> configuration,
            ILogger logger)
            : base(repository, policies, logger)
        {
            this.cache = cache;
            this.wechatRepository = wechatRepository;
            this.loginRepository = loginRepository;
            this.entitlementRepository = entitlementRepository;
            this.openIDManager = openIDManager;
            this.tokenBuilder = tokenBuilder;
            this.configuration = configuration;
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

        public async Task<OperationResult<User>> LoginAsync(string username, string password, OpenIDKind? kind = null)
        {
            if (!kind.HasValue)
            {
                kind = DefaultOpenIDKind;
            }

            var login = await this.loginRepository.Value.GetAsync(
                Login.BuildPartitionKey(kind.Value),
                Login.BuildRowKey(username));
            if (login == null)
            {
                return new OperationResult<User>(InternalError.NotFound);
            }

            var secret = await this.configuration.Value.GetSettingAsync<string>(Constant.AUTH_SECRET_NAME);
            if (login.Credentials.ToUpper() != SHA.SHA256Hash(password, secret).ToUpper())
            {
                return new OperationResult<User>(InternalError.AuthenticationRequired);
            }

            var userID = login.User;
            var user = await this.Repository.GetAsync(
                User.BuildPartitionKey(userID),
                User.BuildRowKey(userID));
            if (user == null)
            {
                return new OperationResult<User>(InternalError.NotFound);
            }

            return new OperationResult<User>(user);
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
                return new OperationResult<LoginResult>(InternalError.AuthenticationRequired);
            }

            var result = new LoginResult();
            var openid = wechat.Value;
            var login = await this.loginRepository.Value.GetAsync(
                Login.BuildPartitionKey(kind, appID),
                Login.BuildRowKey(openid));
            if (login == null)
            {
                var key = await this.configuration.Value.GetSettingAsync<string>(Constant.AUTH_SECRET_NAME);
                result.OpenID = GenerateClientID(kind, appID, openid, key);
                return new OperationResult<LoginResult>(InternalError.NotFound, result) { Reference = openid };
            }

            var userID = login.User;
            var user = await this.Repository.GetAsync(
                User.BuildPartitionKey(userID),
                User.BuildRowKey(userID));
            if (user == null)
            {
                var key = await this.configuration.Value.GetSettingAsync<string>(Constant.AUTH_SECRET_NAME);
                result.OpenID = GenerateClientID(kind, appID, openid, key);
                return new OperationResult<LoginResult>(InternalError.NotFound, result) { Reference = openid };
            }

            if (user.Disabled)
            {
                return new OperationResult<LoginResult>(InternalError.Locked) { Reference = openid };
            }

            result.User = user;
            result.OpenID = openid;
            return new OperationResult<LoginResult>(result);
        }

        public async Task<string> IssueTokenAsync(IEnumerable<KeyValuePair<string, string>> entitlements)
        {
            var entity = await this.GetEntityAsync();
            var userID = entity.GetID();
            var records = await this.entitlementRepository.Value.GetAsync(Entitlement.BuildPartitionKey(userID));
            var es = records.Select(r => new KeyValuePair<string, string>(r.RowKey, r.Value)).ToList();
            if (entitlements != null && entitlements.Any())
            {
                es.AddRange(entitlements);
            }
            return await this.tokenBuilder.Value.BuildAsync(
                userID.ToKey(),
                roles: entity.Roles.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
                entitlements: es,
                validHours: await this.GetTokenValidHoursAsync());
        }

        public async Task<OperationResult<Guid>> GetOrRegisterAsync(string mobile, string ip = null)
        {
            var login = await this.loginRepository.Value.GetAsync(
                    Login.BuildPartitionKey(OpenIDKind.SMS),
                    Login.BuildRowKey(mobile));
            if (login != null)
            {
                return new OperationResult<Guid>(login.User);
            }

            var registations = OpenIDRegistration.Build(mobile);
            var newUser = await this.RegisterAsync(null, registations, ip);
            if (!newUser.IsSuccess)
            {
                return new OperationResult<Guid>(newUser);
            }

            return new OperationResult<Guid>(newUser.Result.GetID());
        }

        public async Task<OperationResult<User>> RegisterAsync(Guid? userID, IEnumerable<OpenIDRegistration> registrations, string ip, bool ignoreDuplication = false)
        {
            var newUser = false;
            var channels = new Dictionary<Guid, IEnumerable<OpenID>>();
            var passiveUserID = new List<Guid>();

            foreach (var registration in registrations)
            {
                var login = await this.loginRepository.Value.GetAsync(
                    Login.BuildPartitionKey(registration.Kind, registration.App),
                    Login.BuildRowKey(registration.Identity));
                if (login != null)
                {
                    if (!channels.ContainsKey(login.User))
                    {
                        var c = await this.openIDManager.Value.GetChannelsAsync(login.User);
                        channels.Add(login.User, c);
                    }

                    var count = registration.Kind switch
                    {
                        (int)OpenIDKind.PhoneCall => channels[login.User].Count(c => c.GetKind() != (int)OpenIDKind.PhoneCall),
                        (int)OpenIDKind.SMS => channels[login.User].Count(c => c.GetKind() != (int)OpenIDKind.SMS),
                        _ => channels[login.User].Count(c => c.GetKind() != (int)OpenIDKind.SMS && c.GetKind() != registration.Kind),
                    };
                    if (!ignoreDuplication && count > 1)
                    {
                        // REMARK (5he11) 因SMS和PhoneCall其实等同，所以过滤掉其中1个之后如果还有其他通道，则表示这是一个既有用户
                        return new OperationResult<User>(InternalError.Conflict);
                    }

                    // REMARK (5he11) 否则可能是因为用户被动注册，如仅被注册了手机号码通道，无实际载体通道，此时应该合并当前注册与被动注册的用户
                    registration.OverrideIfExists = true;
                    if (!passiveUserID.Contains(login.User))
                    {
                        passiveUserID.Add(login.User);
                    }
                }
                else
                {
                    registration.OverrideIfExists = false;
                }
            }

            if (!userID.HasValue)
            {
                userID = passiveUserID.Count == 1 ? passiveUserID[0] : Guid.NewGuid();
            }

            var existing = await this.Repository.GetAsync(User.BuildPartitionKey(userID.Value), User.BuildRowKey(userID.Value));
            newUser = existing == null;

            foreach (var registration in registrations)
            {
                registration.User = userID.Value;
            }

            await this.openIDManager.Value.RegisterAsync(registrations);

            var secret = await this.configuration.Value.GetSettingAsync<string>(Constant.AUTH_SECRET_NAME);
            var logins = new List<Login>();
            foreach (var registration in registrations)
            {
                if (registration.Kind == (int)OpenIDKind.Username)
                {
                    registration.Credentials = SHA.SHA256Hash(registration.Credentials, secret);
                }

                logins.Add(new Login
                {
                    PartitionKey = Login.BuildPartitionKey(registration.Kind, registration.App),
                    RowKey = Login.BuildRowKey(registration.Identity),
                    User = userID.Value,
                    Credentials = registration.Credentials,
                });
            }
            await this.loginRepository.Value.CreateAsync(logins, true);

            if (newUser)
            {
                var newuser = new User
                {
                    PartitionKey = User.BuildPartitionKey(userID.Value),
                    RowKey = User.BuildRowKey(userID.Value),
                    Disabled = false,
                    FirstIP = ip,
                    LastIP = ip,
                    Roles = Role.Customer,
                };

                await this.Repository.CreateAsync(newuser, true);
            }

            var result = await this.Repository.GetAsync(
                User.BuildPartitionKey(userID.Value),
                User.BuildRowKey(userID.Value));
            return new OperationResult<User>(result);
        }

        public async Task<IEnumerable<OpenID>> GetChannelsAsync(OpenIDKind kind, string app)
            => (await this.GetChannelsAsync(kind)).Where(c => c.GetApp() == app);

        public async Task<IEnumerable<OpenID>> GetChannelsAsync(OpenIDKind kind)
            => await this.openIDManager.Value.GetChannelsAsync(Guid.Parse(this.RowKey), (int)kind);

        public async Task<OperationResult<User>> ApplyAsync(string role, object parameter)
        {
            if (String.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentNullException(nameof(role));
            }

            role = role.ToUpperInvariant();

            var user = await this.GetEntityAsync();
            if (user == null)
            {
                return new OperationResult<User>(InternalError.NotFound);
            }

            var result = await this.OnApplyAsync(user, role, parameter);
            if (result.IsSuccess || result.Code == InternalError.Locked)
            {
                user.AddRole(role);
                if (result.Code == InternalError.Locked)
                {
                    user.Disabled = true;
                }
                else
                {
                    user.Disabled = false;
                }

                await this.SaveEntityAsync();
                return new OperationResult<User>(user);
            }
            else
            {
                return new OperationResult<User>(result);
            }
        }

        protected virtual Task<OperationResult> OnApplyAsync(User user, string role, object data)
            => Task.FromResult(OperationResult.Success);

        protected virtual Task<ushort> GetTokenValidHoursAsync() => Task.FromResult<ushort>(8);

        private static string GenerateClientID(OpenIDKind kind, string appID, string openID, string key)
        {
            if (String.IsNullOrWhiteSpace(openID))
            {
                return null;
            }

            return AES.Encrypt($"{(int)kind}{ClientIDSplitString}{appID}{ClientIDSplitString}{openID}", key);
        }
    }
}
