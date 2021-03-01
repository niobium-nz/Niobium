namespace Cod.Channel
{
    public class LoginCommandParameter
    {
        public LoginCommandParameter()
            : this(null, null, null, false, null)
        {
        }

        public LoginCommandParameter(string scheme)
            : this(scheme, null, null, false, null)
        {
        }

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

        public string Scheme { get; set; }

        public string Username { get; set; }

        public bool Remember { get; set; }

        public string Password { get; set; }

        public string ReturnUrl { get; set; }
    }
}
