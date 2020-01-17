namespace Cod.Channel
{
    public class LoginCommandParameter
    {
        public LoginCommandParameter(string username, string password)
            : this(username, password, false, null)
        {
        }

        public LoginCommandParameter(string username, string password, bool remember)
            : this(username, password, remember, null)
        {
        }

        public LoginCommandParameter(string username, string password, bool remember, string returnUrl)
        {
            this.Username = username;
            this.Password = password;
            this.ReturnUrl = returnUrl;
            this.Remember = remember;
        }

        public string Username { get; private set; }

        public bool Remember { get; private set; }

        public string Password { get; private set; }

        public string ReturnUrl { get; private set; }
    }
}
