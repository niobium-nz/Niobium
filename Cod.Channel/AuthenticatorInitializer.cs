using System.Threading.Tasks;

namespace Cod.Channel
{
    internal class AuthenticatorInitializer : IBootstrapper
    {
        private readonly IAuthenticator authenticator;

        public AuthenticatorInitializer(IAuthenticator authenticator) => this.authenticator = authenticator;

        public async Task InitializeAsync() => await this.authenticator.InitializeAsync();
    }
}
