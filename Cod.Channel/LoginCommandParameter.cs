namespace Cod.Channel
{
    public class LoginCommandParameter
    {
        public LoginCommandParameter(string username, string password)
            : this(username, password, null)
        {
        }

        public LoginCommandParameter(string username, string password, string returnUrl)
        {
            this.Username = username;
            this.Password = password;
            this.ReturnUrl = returnUrl;
        }

        public string Username { get; private set; }

        public string Password { get; private set; }

        public string ReturnUrl { get; private set; }
    }
}
