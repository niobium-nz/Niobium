namespace Cod.Channel
{
    public class LoginCommandParameter
    {
        public LoginCommandParameter(string scheme, string username, string password)
            : this(scheme, username, password, false, null)
        {
        }

        public LoginCommandParameter(string scheme, string username, string password, bool remember)
            : this(scheme, username, password, remember, null)
        {
        }

        public LoginCommandParameter(string scheme, string username, string password, bool remember, string returnUrl)
        {
            this.Scheme = scheme;
            this.Username = username;
            this.Password = password;
            this.ReturnUrl = returnUrl;
            this.Remember = remember;
        }

        public string Scheme { get; private set; }

        public string Username { get; private set; }

        public bool Remember { get; private set; }

        public string Password { get; private set; }

        public string ReturnUrl { get; private set; }
    }
}
