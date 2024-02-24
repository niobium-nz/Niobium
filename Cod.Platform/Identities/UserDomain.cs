using Cod.Platform.Authentication;
using Cod.Platform.Authorization;
using Cod.Platform.Database;
using Cod.Platform.Finance;
using Cod.Platform.Locking;
using Cod.Platform.Tenants.Wechat;
using Microsoft.Extensions.Logging;

namespace Cod.Platform.Identities
{
    public class UserDomain : AccountableDomain<User>, IImpedable
    {
        private const string ClientIDSplitString = "###";
        public const OpenIDKind DefaultOpenIDKind = OpenIDKind.Username;

        private readonly Lazy<IRepository<WechatEntity>> wechatRepository;
        private readonly Lazy<IRepository<Login>> loginRepository;
        private readonly Lazy<IRepository<Entitlement>> entitlementRepository;
        private readonly Lazy<IOpenIDManager> openIDManager;
        private readonly Lazy<IRepository<OpenID>> openIDRepository;
        private readonly Lazy<ITokenBuilder> tokenBuilder;
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly Lazy<IEnumerable<IImpedimentPolicy>> impedimentPolicies;

        public IEnumerable<IImpedimentPolicy> ImpedimentPolicies => impedimentPolicies.Value;

        public UserDomain(
            Lazy<IRepository<User>> repository,
            Lazy<IRepository<WechatEntity>> wechatRepository,
            Lazy<IRepository<Login>> loginRepository,
            Lazy<IRepository<Entitlement>> entitlementRepository,
            Lazy<IOpenIDManager> openIDManager,
            Lazy<IRepository<OpenID>> openIDRepository,
            Lazy<ITokenBuilder> tokenBuilder,
            Lazy<IConfigurationProvider> configuration,
            Lazy<IEnumerable<IImpedimentPolicy>> impedimentPolicies,
            Lazy<IQueryableRepository<Transaction>> transactionRepo,
            Lazy<IQueryableRepository<Accounting>> accountingRepo,
            Lazy<IEnumerable<IAccountingAuditor>> auditors,
            Lazy<ICacheStore> cache,
            ILogger logger)
            : base(repository, transactionRepo, accountingRepo, auditors, cache, logger)
        {
            this.wechatRepository = wechatRepository;
            this.loginRepository = loginRepository;
            this.entitlementRepository = entitlementRepository;
            this.openIDManager = openIDManager;
            this.openIDRepository = openIDRepository;
            this.tokenBuilder = tokenBuilder;
            this.configuration = configuration;
            this.impedimentPolicies = impedimentPolicies;
        }

        public override string AccountingPrincipal => RowKey;

        public string GetImpedementID()
        {
            return Cod.Model.User.GetImpedementID(new StorageKey
            {
                PartitionKey = PartitionKey,
                RowKey = RowKey,
            });
        }

        public async Task<OperationResult<User>> LoginAsync(string username, string password, OpenIDKind? kind = null)
        {
            if (!kind.HasValue)
            {
                kind = DefaultOpenIDKind;
            }

            Login login = await loginRepository.Value.RetrieveAsync(
                Cod.Model.Login.BuildPartitionKey(kind.Value),
                Cod.Model.Login.BuildRowKey(username));
            if (login == null)
            {
                return new OperationResult<User>(Cod.InternalError.NotFound);
            }

            if (login.Credentials.ToUpperInvariant() != password.ToUpperInvariant())
            {
                return new OperationResult<User>(Cod.InternalError.AuthenticationRequired);
            }

            Guid userID = login.User;
            User user = await Repository.RetrieveAsync(
                Cod.Model.User.BuildPartitionKey(userID),
                Cod.Model.User.BuildRowKey(userID));
            return user == null ? new OperationResult<User>(Cod.InternalError.NotFound) : new OperationResult<User>(user);
        }

        public async Task<OperationResult<LoginResult>> LoginAsync(OpenIDKind kind, string appID, string authCode)
        {
            if (kind != OpenIDKind.Wechat)
            {
                throw new NotSupportedException();
            }

            WechatEntity wechat = await wechatRepository.Value.RetrieveAsync(
                    WechatEntity.BuildOpenIDPartitionKey(appID),
                    WechatEntity.BuildOpenIDRowKey(authCode));
            if (wechat == null || string.IsNullOrWhiteSpace(wechat.Value))
            {
                return new OperationResult<LoginResult>(Cod.InternalError.AuthenticationRequired);
            }

            LoginResult result = new();
            string openid = wechat.Value;
            Login login = await loginRepository.Value.RetrieveAsync(
                Cod.Model.Login.BuildPartitionKey(kind, appID),
                Cod.Model.Login.BuildRowKey(openid));
            if (login == null)
            {
                string key = await configuration.Value.GetSettingAsync<string>(Constant.AUTH_SECRET_NAME);
                result.OpenID = GenerateClientID(kind, appID, openid, key);
                return new OperationResult<LoginResult>(Cod.InternalError.NotFound, result) { Reference = openid };
            }

            Guid userID = login.User;
            User user = await Repository.RetrieveAsync(
                Cod.Model.User.BuildPartitionKey(userID),
                Cod.Model.User.BuildRowKey(userID));
            if (user == null)
            {
                string key = await configuration.Value.GetSettingAsync<string>(Constant.AUTH_SECRET_NAME);
                result.OpenID = GenerateClientID(kind, appID, openid, key);
                return new OperationResult<LoginResult>(Cod.InternalError.NotFound, result) { Reference = openid };
            }

            if (user.Disabled)
            {
                return new OperationResult<LoginResult>(Cod.InternalError.Locked) { Reference = openid };
            }

            result.User = user;
            result.OpenID = openid;
            return new OperationResult<LoginResult>(result);
        }

        public async Task<string> IssueTokenAsync(IEnumerable<KeyValuePair<string, string>> entitlements)
        {
            User entity = await GetEntityAsync();
            Guid userID = entity.GetID();
            List<Entitlement> records = await entitlementRepository.Value.GetAsync(Cod.Model.Entitlement.BuildPartitionKey(userID)).ToListAsync();
            List<KeyValuePair<string, string>> es = records.Select(r => new KeyValuePair<string, string>(r.RowKey, r.Value)).ToList();
            if (entitlements != null && entitlements.Any())
            {
                es.AddRange(entitlements);
            }
            return await tokenBuilder.Value.BuildAsync(
                userID.ToKey(),
                roles: entity.Roles.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
                entitlements: es,
                validHours: await GetTokenValidHoursAsync());
        }

        public async Task<OperationResult<Guid>> GetOrRegisterAsync(string mobile, string ip = null)
        {
            Login login = await loginRepository.Value.RetrieveAsync(
                    Cod.Model.Login.BuildPartitionKey(OpenIDKind.SMS),
                    Cod.Model.Login.BuildRowKey(mobile));
            if (login != null)
            {
                return new OperationResult<Guid>(login.User);
            }

            IEnumerable<OpenIDRegistration> registations = OpenIDRegistration.Build(mobile);
            OperationResult<User> newUser = await RegisterAsync(null, registations, ip);
            return !newUser.IsSuccess ? new OperationResult<Guid>(newUser) : new OperationResult<Guid>(newUser.Result.GetID());
        }

        public async Task<OperationResult<User>> RegisterAsync(Guid? userID, IEnumerable<OpenIDRegistration> registrations, string ip, RegisterOptionOnDuplication actionOnDuplication = RegisterOptionOnDuplication.RefuseRegistration)
        {
            bool newUser = false;
            Dictionary<Guid, IEnumerable<OpenID>> channels = new();
            List<OpenID> openIDsToRemove = new();
            List<Guid> passiveUserID = new();

            foreach (OpenIDRegistration registration in registrations)
            {
                Login login = await loginRepository.Value.RetrieveAsync(
                    Cod.Model.Login.BuildPartitionKey(registration.Kind, registration.App),
                    Cod.Model.Login.BuildRowKey(registration.Identity));
                if (login != null)
                {
                    IEnumerable<OpenID> openIDs = null;
                    if (!channels.ContainsKey(login.User))
                    {
                        openIDs = await openIDManager.Value.GetChannelsAsync(login.User).ToListAsync();
                        channels.Add(login.User, openIDs);
                    }

                    int count = registration.Kind switch
                    {
                        (int)OpenIDKind.PhoneCall => channels[login.User].Count(c => c.GetKind() != (int)OpenIDKind.PhoneCall),
                        (int)OpenIDKind.SMS => channels[login.User].Count(c => c.GetKind() != (int)OpenIDKind.SMS),
                        _ => channels[login.User].Count(c => c.GetKind() != (int)OpenIDKind.SMS && c.GetKind() != registration.Kind),
                    };
                    if (count > 1)
                    {
                        // REMARK (5he11) 因SMS和PhoneCall其实等同，所以过滤掉其中1个之后如果还有其他通道，则表示这是一个既有用户
                        switch (actionOnDuplication)
                        {
                            case RegisterOptionOnDuplication.RefuseRegistration:
                                return new OperationResult<User>(Cod.InternalError.Conflict);
                            case RegisterOptionOnDuplication.RestoreExistingIdentity:
                                if (registration.Kind is ((int)OpenIDKind.PhoneCall) or ((int)OpenIDKind.SMS) or ((int)OpenIDKind.Email))
                                {
                                    // REMARK (5he11) 如果选择还原用户身份，则仅相信非社交媒体账号作为还原凭据
                                    userID = login.User;
                                }
                                else
                                {
                                    continue;
                                }
                                break;
                            case RegisterOptionOnDuplication.CreateNewIdentity:
                                userID = Guid.NewGuid();
                                break;
                            default:
                                break;
                        }
                    }

                    if (userID.HasValue && login.User != userID.Value)
                    {
                        // REMARK (5he11) 如果一个现有用户去“偷”另外一个用户的Login的话，则不应该为了优化性能而强制使用0号记录，应该让程序自动判断使用哪个Offset，否则可能导致该现有的既有的第0个OpenID被覆盖
                        registration.ForceOffset0 = false;

                        if (openIDs != null)
                        {
                            foreach (OpenID openid in openIDs)
                            {
                                // REMARK (5he11) 遇到“偷”Login的情形，需要查出被偷的Login所对应的OpenID中是否存在与即将创建的OpenID相同的既有OpenID，如果存在，则需要删除
                                if (openid.Identity == registration.Identity && !openIDsToRemove.Any(o => o.GetKey() == openid.GetKey()))
                                {
                                    openIDsToRemove.Add(openid);
                                }
                            }
                        }
                    }
                    else
                    {
                        // REMARK (5he11) 否则可能是因为用户被动注册，如仅被注册了手机号码通道，无实际载体通道，此时应该合并当前注册与被动注册的用户
                        registration.ForceOffset0 = true;
                        if (!passiveUserID.Contains(login.User))
                        {
                            passiveUserID.Add(login.User);
                        }
                    }
                }
                else
                {
                    if (actionOnDuplication == RegisterOptionOnDuplication.RefuseRegistration)
                    {
                        registration.ForceOffset0 = false;
                    }
                }
            }

            if (!userID.HasValue)
            {
                userID = passiveUserID.Count == 1 ? passiveUserID[0] : Guid.NewGuid();
            }

            User existing = await Repository.RetrieveAsync(Cod.Model.User.BuildPartitionKey(userID.Value), Cod.Model.User.BuildRowKey(userID.Value));
            newUser = existing == null;

            foreach (OpenIDRegistration registration in registrations)
            {
                registration.User = userID.Value;
            }

            await openIDManager.Value.RegisterAsync(registrations);

            List<Login> newLogins = new();
            foreach (OpenIDRegistration registration in registrations)
            {
                if (registration.Kind == (int)OpenIDKind.Username)
                {
                    registration.Credentials = registration.Credentials.Trim();
                }

                newLogins.Add(new Login
                {
                    PartitionKey = Cod.Model.Login.BuildPartitionKey(registration.Kind, registration.App),
                    RowKey = Cod.Model.Login.BuildRowKey(registration.Identity),
                    User = userID.Value,
                    Credentials = registration.Credentials,
                });
            }
            _ = await loginRepository.Value.CreateAsync(newLogins, true);

            if (openIDsToRemove.Count > 0)
            {
                // REMARK (5he11) 这些记录本质上是因为某用户添加绑定，因此“自动”从其他已有用户处“偷”过来的Login，因为其他用户被“偷”Login，所以这些用户被偷的Login的相关OpenID应该删除才对
                await openIDRepository.Value.DeleteAsync(openIDsToRemove, successIfNotExist: true);
            }

            if (newUser)
            {
                User newuser = new()
                {
                    PartitionKey = Cod.Model.User.BuildPartitionKey(userID.Value),
                    RowKey = Cod.Model.User.BuildRowKey(userID.Value),
                    Disabled = false,
                    FirstIP = ip,
                    LastIP = ip,
                    Roles = Role.Customer,
                };

                await Repository.CreateAsync(newuser, true);
            }

            User result = await Repository.RetrieveAsync(
                Cod.Model.User.BuildPartitionKey(userID.Value),
                Cod.Model.User.BuildRowKey(userID.Value));
            return new OperationResult<User>(result);
        }

        public async IAsyncEnumerable<OpenID> GetChannelsAsync(OpenIDKind kind, string app)
        {
            IAsyncEnumerable<OpenID> channels = GetChannelsAsync(kind);
            await foreach (OpenID channel in channels)
            {
                if (channel.GetApp() == app)
                {
                    yield return channel;
                }
            }
        }

        public IAsyncEnumerable<OpenID> GetChannelsAsync(OpenIDKind kind, CancellationToken cancellationToken = default)
        {
            return openIDManager.Value.GetChannelsAsync(Guid.Parse(RowKey), (int)kind, cancellationToken);
        }

        public async Task<OperationResult<User>> ApplyAsync(string role, object parameter)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentNullException(nameof(role));
            }

            role = role.ToUpperInvariant();

            User user = await GetEntityAsync();
            if (user == null)
            {
                return new OperationResult<User>(Cod.InternalError.NotFound);
            }

            if (user.GetRoles().Contains(role))
            {
                return new OperationResult<User>(user);
            }

            OperationResult result = await OnApplyAsync(user, role, parameter);
            if (result.IsSuccess || result.Code == Cod.InternalError.Locked)
            {
                _ = user.AddRole(role);
                user.Disabled = result.Code == Cod.InternalError.Locked;

                await SaveEntityAsync();
                return new OperationResult<User>(user);
            }
            else
            {
                return new OperationResult<User>(result);
            }
        }

        protected virtual Task<OperationResult> OnApplyAsync(User user, string role, object data)
        {
            return Task.FromResult(OperationResult.Success);
        }

        protected virtual Task<ushort> GetTokenValidHoursAsync()
        {
            return Task.FromResult<ushort>(8);
        }

        private static string GenerateClientID(OpenIDKind kind, string appID, string openID, string key)
        {
            return string.IsNullOrWhiteSpace(openID)
                ? null
                : AES.Encrypt($"{(int)kind}{ClientIDSplitString}{appID}{ClientIDSplitString}{openID}", key);
        }
    }
}
