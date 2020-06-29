using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    internal class DefaultUserService : IUserService
    {
        private readonly Lazy<IRepository<Model.Login>> loginRepository;
        private readonly Lazy<IDomainRepository<UserDomain, Model.User>> userDomainRepository;
        private readonly Lazy<IRepository<Model.User>> userRepository;
        private readonly Lazy<IOpenIDManager> openIDManager;

        public DefaultUserService(
            Lazy<IRepository<Model.Login>> loginRepository,
            Lazy<IDomainRepository<UserDomain, Model.User>> userDomainRepository,
            Lazy<IRepository<Model.User>> userRepository,
            Lazy<IOpenIDManager> openIDManager)
        {
            this.loginRepository = loginRepository;
            this.userDomainRepository = userDomainRepository;
            this.userRepository = userRepository;
            this.openIDManager = openIDManager;
        }

        public async Task<UserDomain> GetOrCreateAsync(string mobile, string remoteIP)
        {
            if (String.IsNullOrWhiteSpace(mobile))
            {
                throw new ArgumentNullException(nameof(mobile));
            }

            mobile = mobile.Trim();

            var login = await this.loginRepository.Value.GetAsync(Login.BuildPartitionKey(OpenIDKind.SMS), Login.BuildRowKey(mobile));
            if (login != null)
            {
                return await this.userDomainRepository.Value.GetAsync(User.BuildPartitionKey(login.User), User.BuildRowKey(login.User));
            }

            return await CreateAsync(Guid.NewGuid(), OpenIDKind.SMS, mobile, remoteIP);
        }

        public async Task<UserDomain> GetOrCreateAsync(string mobile, OpenIDKind kind, string appID, string openID, string remoteIP)
        {
            if (String.IsNullOrWhiteSpace(mobile))
            {
                throw new ArgumentNullException(nameof(mobile));
            }

            mobile = mobile.Trim();

            var mobileLogin = await this.loginRepository.Value.GetAsync(Login.BuildPartitionKey(OpenIDKind.SMS), Login.BuildRowKey(mobile));
            var openIDLogin = await this.loginRepository.Value.GetAsync(Login.BuildPartitionKey(kind, appID), Login.BuildRowKey(openID));
            if (mobileLogin != null && openIDLogin != null)
            {
                if (mobileLogin.User == openIDLogin.User)
                {
                    return await this.userDomainRepository.Value.GetAsync(User.BuildPartitionKey(openIDLogin.User), User.BuildRowKey(openIDLogin.User));
                }
                else
                {
                    // REMARK (5he11) 这种情况是用户曾经扫码后创建了用户但是未验证手机号码，而后被派件后创建了基于手机号的另外一个新的用户，此时应该废掉那个未验证手机号码的用户
                    openIDLogin = null;
                }
            }

            if (mobileLogin != null)
            {
                return await CreateAsync(mobileLogin.User, kind, appID, openID, remoteIP, createUser: false);
            }
            else if (openIDLogin != null)
            {
                return await CreateAsync(openIDLogin.User, OpenIDKind.SMS, mobile, remoteIP, createUser: false);
            }
            else
            {
                var userID = Guid.NewGuid();
                await CreateAsync(userID, OpenIDKind.SMS, mobile, remoteIP, createUser: false);
                return await CreateAsync(userID, kind, appID, openID, remoteIP, createUser: true);
            }
        }

        protected virtual async Task<UserDomain> CreateAsync(Guid userID, OpenIDKind kind, string identifier, string remoteIP, bool createUser = true)
            => await CreateAsync(userID, kind, null, identifier, remoteIP, createUser);

        protected virtual async Task<UserDomain> CreateAsync(Guid userID, OpenIDKind kind, string app, string identifier, string remoteIP, bool createUser = true)
        {
            IEnumerable<OpenIDRegistration> openids;
            if (kind == OpenIDKind.SMS || kind == OpenIDKind.PhoneCall)
            {
                openids = new[]
                {
                    new OpenIDRegistration
                    {
                        Account = userID.ToKey(),
                        Identity = identifier,
                        Kind = (int)OpenIDKind.PhoneCall,
                        OverrideIfExists = true,
                    },
                    new OpenIDRegistration
                    {
                        Account = userID.ToKey(),
                        Identity = identifier,
                        Kind = (int)OpenIDKind.SMS,
                        OverrideIfExists = true,
                    },
                };
            }
            else
            {
                openids = new[]
                {
                    new OpenIDRegistration
                    {
                        Account = userID.ToKey(),
                        App = app,
                        Identity = identifier,
                        Kind = (int)kind,
                        OverrideIfExists = true,
                    },
                };
            }

            await this.openIDManager.Value.RegisterAsync(openids);

            await this.loginRepository.Value.CreateAsync(
                new[]
                {
                    new Model.Login
                    {
                        User = userID,
                        PartitionKey = Login.BuildPartitionKey(kind, app),
                        RowKey = Login.BuildRowKey(identifier),
                    },
                },
                true);

            if (createUser)
            {
                var user = new Model.User
                {
                    PartitionKey = User.BuildPartitionKey(userID),
                    RowKey = User.BuildRowKey(userID),
                    Disabled = false,
                    FirstIP = remoteIP,
                    LastIP = remoteIP,
                    Roles = Role.Customer,
                };
                await this.userRepository.Value.CreateAsync(new[] { user }, true);
                return await this.userDomainRepository.Value.GetAsync(user);
            }
            else
            {
                return await this.userDomainRepository.Value.GetAsync(User.BuildPartitionKey(userID), User.BuildRowKey(userID));
            }
        }
    }
}
