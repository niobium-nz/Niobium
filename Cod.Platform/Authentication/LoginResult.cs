using Cod.Platform.Identities;

namespace Cod.Platform.Authentication
{
    public class LoginResult
    {
        public User User { get; set; }

        public string OpenID { get; set; }
    }
}
