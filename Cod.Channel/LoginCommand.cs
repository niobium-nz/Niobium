using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Channel
{
    internal class LoginCommand : GenericCommand<LoginCommandParameter>
    {
        private readonly IAuthenticator authenticator;
        private readonly INavigator navigator;

        public override string CommandID => Constants.LOGIN_COMMAND_ID;

        public LoginCommand(IAuthenticator authenticator, INavigator navigator)
        {
            this.authenticator = authenticator;
            this.navigator = navigator;
        }

        protected override async Task CoreExecuteAsync(LoginCommandParameter parameter)
        {
            await this.authenticator.AquireTokenAsync(parameter.Username, parameter.Password);
            var returnUrl = parameter.ReturnUrl;
            if (String.IsNullOrEmpty(returnUrl))
            {
                var queryGroups = new Dictionary<string, string>();
                var uri = this.navigator.CurrentUri;
                var index = uri.IndexOf('?');
                if (index >= 0 && uri.Length > index)
                {
                    var querystringLength = uri.Length - index - 1;
                    if (querystringLength > 0)
                    {
                        var querystring = uri.Substring(index + 1, querystringLength);
                        var queries = querystring.Split('&');
                        foreach (var query in queries)
                        {
                            var parts = query.Split('=');
                            if (parts.Length == 2)
                            {
                                queryGroups.Add(parts[0], parts[1]);
                            }
                        }
                    }
                }

                if (queryGroups.ContainsKey("returnUrl"))
                {
                    returnUrl = queryGroups["returnUrl"];
                }
            }

            if (!String.IsNullOrWhiteSpace(returnUrl))
            {
                this.navigator.NavigateTo(returnUrl);
            }
        }
    }
}
